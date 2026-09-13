using Microsoft.EntityFrameworkCore;
using StackUnderflow.Data;

namespace StackUnderflow.Services;

// Locks threads that have stayed solved past the retention window so they stop
// accepting new answers and comments. Runs on a timer for the life of the app;
// ThreadController.Detail also applies the same rule lazily when a thread is viewed.
public class ThreadAutoLockService(IServiceScopeFactory scopeFactory, ILogger<ThreadAutoLockService> logger)
    : BackgroundService
{
    // How long a thread may stay solved before it locks automatically.
    public static readonly TimeSpan SolvedLockAfter = TimeSpan.FromDays(30);

    // How often the background sweep runs.
    private static readonly TimeSpan SweepInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SweepInterval);

        // Run once at startup, then on every tick.
        do
        {
            try
            {
                await LockStaleThreadsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to auto-lock stale solved threads.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task LockStaleThreadsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var cutoff = DateTime.UtcNow - SolvedLockAfter;

        var locked = await context.SUThreads
            .Where(t => t.IsSolved && !t.IsLocked && t.SolvedAt != null && t.SolvedAt <= cutoff)
            .ExecuteUpdateAsync(setters =>
            {
                setters.SetProperty(t => t.IsLocked, true);
                setters.SetProperty(t => t.LockedByAdmin, true);
            }, ct);

        if (locked > 0)
        {
            logger.LogInformation("Auto-locked {Count} thread(s) solved before {Cutoff:u}.", locked, cutoff);
        }
    }
}
