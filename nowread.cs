         string[] inputClasses = ["Asset"];
         //AutoMapperProfilePerClassGenerator.Generate(allEntities, inputClasses, Path.Combine(outputPath, "AutoMapperProfiles"));

         string outputDir = Path.Combine(
 AppDomain.CurrentDomain.BaseDirectory,
 "Generated",
 "Projections");

         // 3. Path to your EF entities (.cs files under NowBet.AMS.DataAccess\Entities)
         string efEntitiesFolder = @"D:\0-AMS\Automation\Tool\NowBet.AMS.DataAccess\Entities";

         // 4. Generate projection files
         ProjectionGenerator.Generate(allEntities, outputDir, efEntitiesFolder);

         string queryoutputDir = Path.Combine(
AppDomain.CurrentDomain.BaseDirectory,
"Generated",
"QueryGenerated");

         QueryGenerator.Generate_old(allEntities, queryoutputDir, efEntitiesFolder);
