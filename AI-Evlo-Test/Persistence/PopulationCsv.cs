using System.Globalization;
using System.Text;
namespace AI_Evlo_Test.Persistence
{
    internal static class PopulationCsv
    {
        internal static string Export(PopulationDashboardSnapshot snapshot)
        {
            var text = new StringBuilder("Population,Species,RecordingStartedCycle,Cycle,Alive,TotalEver,TopFitness,MeanFitness,MeanAge\r\n");
            string Quote(string value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
            foreach (var sample in snapshot.Series)
            {
                text.Append(Quote(snapshot.Name)).Append(',').Append(Quote(snapshot.Species)).Append(',');
                text.Append(snapshot.RecordingStartedCycle?.ToString(CultureInfo.InvariantCulture)).Append(',');
                text.AppendLine(string.Join(",", sample.Cycle.ToString(CultureInfo.InvariantCulture),
                    sample.Alive.ToString(CultureInfo.InvariantCulture), sample.TotalEver.ToString(CultureInfo.InvariantCulture),
                    sample.TopFitness.ToString("R", CultureInfo.InvariantCulture),
                    sample.MeanFitness.ToString("R", CultureInfo.InvariantCulture), sample.MeanAge.ToString("R", CultureInfo.InvariantCulture)));
            }
            return text.ToString();
        }
    }
}
