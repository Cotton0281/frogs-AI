using System;

namespace AI_Evlo_Test.Objects
{
    internal static class PopulationArchive
    {
        internal static void Add(Population population, ISmartObject member)
        {
            if (population == null)
                throw new ArgumentNullException(nameof(population));
            if (member?.NNetwork == null)
                return;

            int capacity = Math.Max(0, population.SizeLimit / 2);
            if (capacity == 0)
            {
                population.lsBestGenes.Clear();
                return;
            }

            TrimToCapacity(population, capacity);

            double worstFitness = population.lsBestGenes.Count > 0
                ? population.lsBestGenes[population.lsBestGenes.Count - 1].Fitness
                : double.NegativeInfinity;
            if (population.lsBestGenes.Count >= capacity && member.Fitness <= worstFitness)
                return;

            var record = new GenomeRecord
            {
                Fitness = member.Fitness,
                Gene = member.NNetwork.GetGenes(),
                Generation = member.Generation,
                ID = member.ID
            };

            int insertionIndex = population.lsBestGenes.FindIndex(gene => gene.Fitness <= record.Fitness);
            if (insertionIndex < 0)
                population.lsBestGenes.Add(record);
            else
                population.lsBestGenes.Insert(insertionIndex, record);

            TrimToCapacity(population, capacity);
        }

        private static void TrimToCapacity(Population population, int capacity)
        {
            while (population.lsBestGenes.Count > capacity)
            {
                int lastIndex = population.lsBestGenes.Count - 1;
                population.lsBestGenes[lastIndex].Gene = null;
                population.lsBestGenes.RemoveAt(lastIndex);
            }
        }
    }
}
