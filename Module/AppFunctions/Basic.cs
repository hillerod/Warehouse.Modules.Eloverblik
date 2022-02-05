using Bygdrift.Warehouse;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Module.AppFunctions
{
    public class Basic
    {
        [FunctionName(nameof(PeriodicClean))]  //Called each monday 12AM
        public static async Task PeriodicClean([TimerTrigger("0 0 12 * * 1")] TimerInfo timerInfo, [DurableClient] IDurableClient client)
        {
            await client.CleanEntityStorageAsync(removeEmptyEntities: true, releaseOrphanedLocks: true, default);
            var source = new CancellationTokenSource();
            var instances = await client.ListInstancesAsync(new OrchestrationStatusQueryCondition { CreatedTimeFrom = DateTime.MinValue, CreatedTimeTo = DateTime.UtcNow.AddDays(-7), }, source.Token);
            foreach (var instance in instances.DurableOrchestrationState)
                if (instance.RuntimeStatus == OrchestrationRuntimeStatus.Completed)
                    try
                    {
                        await client.PurgeInstanceHistoryAsync(instance.InstanceId);
                    }
                    catch (Exception)
                    {
                    }
        }

        public static bool IsQualifiedInstance(AppBase app, string instanceId)
        {
            var qualifiedInstanceId = app.Config["QualifiedInstanceId"];
            if (qualifiedInstanceId != instanceId)
            {
                app.Log.LogInformation($"Has rejected extra call from instance {instanceId}.");
                return false;
            }
            return true;
        }

        public static async Task<bool> IsRunning(AppBase app, IDurableClient client)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(AppBase));

            if (app.Config["QualifiedInstanceId"] == null)
                return false;

            var runningInstances = await client.ListInstancesAsync(new OrchestrationStatusQueryCondition
            {
                RuntimeStatus = new OrchestrationRuntimeStatus[] { OrchestrationRuntimeStatus.Pending }
            }, CancellationToken.None);

            if (runningInstances.DurableOrchestrationState.Any())
                return runningInstances.DurableOrchestrationState.Any(o => o.InstanceId == app.Config["QualifiedInstanceId"]);

            return false;
        }

        public static async Task<int> TerminateInstances(IDurableClient client)
        {
            var existingInstances = await client.ListInstancesAsync(new OrchestrationStatusQueryCondition
            {
                RuntimeStatus = new OrchestrationRuntimeStatus[] { OrchestrationRuntimeStatus.Pending, OrchestrationRuntimeStatus.Running, OrchestrationRuntimeStatus.ContinuedAsNew }
            }, CancellationToken.None);

            // Make a hash set to track all instances we got back
            var running = new HashSet<string>(existingInstances.DurableOrchestrationState.Select(s => s.InstanceId));
            var instanceCount = running.Count;
            if (instanceCount == 0)
                return 0;

            // Send terminate commands to all instances in parallel
            var terminateTasks = new List<Task>();
            foreach (string instanceId in running)
                terminateTasks.Add(client.TerminateAsync(instanceId, "None"));

            await Task.WhenAll(terminateTasks);

            while (true)  // Wait for the orchestrations to actually terminate
            {
                DurableOrchestrationStatus[] results = await Task.WhenAll(running.Select(id => client.GetStatusAsync(id)));

                foreach (DurableOrchestrationStatus status in results)
                    if (status != null && status.RuntimeStatus != OrchestrationRuntimeStatus.Pending && status.RuntimeStatus != OrchestrationRuntimeStatus.Running)  // Remove any terminated or completed instances from the hashset
                        running.Remove(status.InstanceId);

                if (running.Count == 0)
                    break;

                await Task.Delay(TimeSpan.FromSeconds(1));  // Still waiting for some instances to complete
            }

            return instanceCount;
        }
    }
}
