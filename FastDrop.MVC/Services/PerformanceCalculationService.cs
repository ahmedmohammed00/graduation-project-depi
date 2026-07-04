using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FastDrop.MVC.Models;

namespace FastDrop.MVC.Services
{
    public class PerformanceCalculationService : IPerformanceCalculationService
    {
        private readonly DmsDbContext _context;

        public PerformanceCalculationService(DmsDbContext context)
        {
            _context = context;
        }

        public async Task<PerformanceScore> CalculateDriverPerformanceAsync(int driverId)
        {
            // 1. Get all attempts for the driver
            var attempts = await _context.ShipmentAttempts
                .Include(a => a.Shipment)
                .Where(a => a.DriverId == driverId)
                .ToListAsync();

            // 2. Get all feedback for the driver
            var feedbacks = await _context.Feedbacks
                .Where(f => f.DriverId == driverId)
                .ToListAsync();

            // Metrics
            int totalAttempts = attempts.Count;
            var successfulAttempts = attempts.Where(a => a.Status == "Delivered").ToList();
            int totalSuccessfulAttempts = successfulAttempts.Count;

            // Delivery Success Rate (40%)
            decimal successRate = totalAttempts > 0 
                ? ((decimal)totalSuccessfulAttempts / totalAttempts) * 100 
                : 0;

            // Customer Satisfaction (40%)
            decimal avgRating = feedbacks.Any() 
                ? feedbacks.Average(f => f.Rating) ?? 0m 
                : 0m;
            decimal satisfactionScore = avgRating > 0 ? (avgRating / 5.0m) * 100 : 0;

            // Efficiency / First-Attempt Success (20%)
            // Get unique shipments delivered
            var successfulShipmentIds = successfulAttempts.Select(a => a.ShipmentId).Distinct().ToList();
            int firstAttemptSuccesses = 0;

            foreach (var shipmentId in successfulShipmentIds)
            {
                var shipmentAttempts = attempts.Where(a => a.ShipmentId == shipmentId).OrderBy(a => a.AttemptTime).ToList();
                if (shipmentAttempts.First().Status == "Delivered")
                {
                    firstAttemptSuccesses++;
                }
            }

            decimal efficiencyScore = totalSuccessfulAttempts > 0 
                ? ((decimal)firstAttemptSuccesses / totalSuccessfulAttempts) * 100 
                : 0;

            // Calculate overall score (Weighted)
            decimal overallScore = (successRate * 0.40m) + (satisfactionScore * 0.40m) + (efficiencyScore * 0.20m);
            overallScore = Math.Round(overallScore, 2);

            // Determine Tier
            string tier;
            if (overallScore >= 90) tier = "Gold";
            else if (overallScore >= 75) tier = "Silver";
            else tier = "Bronze";

            // Save to Database
            var performanceScore = new PerformanceScore
            {
                DriverId = driverId,
                DailyScore = overallScore,
                Tier = tier,
                WeightedHistory = overallScore, // Simplified for now
                RecordedAt = DateTime.UtcNow
            };

            _context.PerformanceScores.Add(performanceScore);
            await _context.SaveChangesAsync();

            return performanceScore;
        }
    }
}
