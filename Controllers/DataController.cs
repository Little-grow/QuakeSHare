using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using System.Linq;
using System.Formats.Asn1;
using BenchmarkDotNet.Exporters.Csv;

namespace QuakeSphere.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataController : ControllerBase
    {
        // Endpoint to import data from CSV and return with earthquake mark
        [HttpGet("import")]
        public IActionResult ImportData()
        {
            // Path to the CSV file (update this to your actual file path)
            string filePath = @"G:\Nasa\space_apps_2024_seismic_detection\space_apps_2024_seismic_detection\data\lunar\test\data\S12_GradeB\xa.s12.00.mhz.1970-01-09HR00_evid00007.csv";
            // string filePath = @"G:\xa.s12.00.mhz.1970-01-09HR00_evid00007.csv";

            // Step 1: Get earthquake mark from AI (mocking AI response with static data for now)
            AiResponse aiResponse = CallAiApi(filePath);

            // Step 2: Import time and velocity data from CSV before and after the earthquake mark (50 points total)
            List<TimeVelocityData> dataPoints = ImportTimeVelocityData(filePath, aiResponse.TimeInSeconds);

            // Step 3: Return the time, velocity, and earthquake mark
            var response = new
            {
                DataPoints = dataPoints,               // Time and velocity points from CSV
                EarthquakeTime = aiResponse.TimeInSeconds  // Earthquake mark from AI
            };

            return Ok(response);
        }

        // Mock method to call AI API and retrieve earthquake mark
        private AiResponse CallAiApi(string fileName)
        {
            // Simulated earthquake time in seconds from AI
            double earthquakeTime = 1.96226415094339;

            return new AiResponse
            {
                TimeInSeconds = earthquakeTime
            };
        }

        // Method to import time and velocity data from CSV file with a fixed number of points before and after earthquake mark
        private List<TimeVelocityData> ImportTimeVelocityData(string filePath, double earthquakeTime)
        {
            List<TimeVelocityData> dataPoints = new List<TimeVelocityData>();

            try
            {
                var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HeaderValidated = null, // Disable header validation to avoid errors if some headers are missing
                    MissingFieldFound = null, // Ignore missing fields
                    IgnoreBlankLines = true
                };

                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<CsvDataRecord>().ToList();

                    // Find the closest point to the earthquake time
                    int earthquakeIndex = records.FindIndex(r => r.TimeRel >= earthquakeTime);
                    int totalPoints = 50; // Set total number of points to fetch (adjust if necessary)

                    // Ensure we get equal points before and after the earthquake time
                    int pointsBefore = 10;
                    int pointsAfter = totalPoints - pointsBefore;

                    // Get points before and after the earthquake time
                    var pointsBeforeEarthquake = records
                        .Skip(Math.Max(0, earthquakeIndex - pointsBefore))
                        .Take(pointsBefore);

                    var pointsAfterEarthquake = records
                        .Skip(earthquakeIndex)
                        .Take(pointsAfter);

                    // Combine the two sets of points
                    var selectedPoints = pointsBeforeEarthquake.Concat(pointsAfterEarthquake).ToList();

                    // Convert CSV records to TimeVelocityData objects
                    foreach (var record in selectedPoints)
                    {
                        dataPoints.Add(new TimeVelocityData
                        {
                            TimeInSeconds = record.TimeRel,
                            Velocity = record.Velocity
                        });
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                throw new Exception($"File not found: {filePath}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Error reading CSV file", ex);
            }

            return dataPoints;
        }
    }

    // Model to represent each CSV data row
    public class CsvDataRecord
    {
        [Name("time_abs(%Y-%m-%dT%H:%M:%S.%f)")]
        public string TimeAbs { get; set; }     // Absolute time

        [Name("time_rel(sec)")]
        public double TimeRel { get; set; }     // Relative time (X-axis)

        [Name("velocity(m/s)")]
        public double Velocity { get; set; }    // Velocity (Y-axis)
    }

    // Request and response models
    public class TimeVelocityData
    {
        public double TimeInSeconds { get; set; }  // Time (X-axis)
        public double Velocity { get; set; }       // Velocity (Y-axis)
    }

    public class AiResponse
    {
        public double TimeInSeconds { get; set; }  // Earthquake time in seconds
    }
}
