using AI_Evlo_Test.Objects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AI_Evlo_Test
{
    public partial class PopulationList : Form
    {
        private Func<PopulationListSnapshot> snapshotProvider;
        public PopulationList()
        {
            InitializeComponent();
            if (WindowBoundsStore.TryGet("PopulationList", out double w, out double h))
                Size = new Size((int)w, (int)h);
            FormClosing += (s, e) => WindowBoundsStore.Save("PopulationList", Width, Height);
        }
        public void SetDataSource(Population population)
        {
            if (population == null)
                throw new ArgumentNullException(nameof(population));

            SetSnapshotProvider(() => PopulationListSnapshot.Capture(population));
        }

        internal void SetSnapshotProvider(Func<PopulationListSnapshot> provider)
        {
            snapshotProvider = provider ?? throw new ArgumentNullException(nameof(provider));
            RefreshList();
        }

        public void RefreshList()
        {
            PopulationListSnapshot snapshot = snapshotProvider?.Invoke();
            if (snapshot == null)
                return;

            dataGridView1.DataSource = snapshot.ArchivedGenes;
            dataGridView2.DataSource = snapshot.Members;
            dataGridView1.Refresh();
            dataGridView2.Refresh();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            RefreshList();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            RefreshList();
        }

        private void dataGridView2_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            var x = e;
            var y = sender;
        }
    }

    internal sealed class PopulationListSnapshot
    {
        internal List<ArchivedGeneRow> ArchivedGenes { get; private set; } = new List<ArchivedGeneRow>();
        internal List<PopulationMemberRow> Members { get; private set; } = new List<PopulationMemberRow>();

        internal static PopulationListSnapshot Capture(Population population)
        {
            return new PopulationListSnapshot
            {
                ArchivedGenes = (population.lsBestGenes ?? new List<GenomeRecord>())
                    .Where(gene => gene != null)
                    .OrderByDescending(gene => gene.Fitness)
                    .Select(gene => new ArchivedGeneRow(gene))
                    .ToList(),
                Members = (population.Members ?? new List<ISmartObject>())
                    .Where(member => member != null)
                    .Select(member => new PopulationMemberRow(member))
                    .ToList()
            };
        }
    }

    internal sealed class ArchivedGeneRow
    {
        internal ArchivedGeneRow(GenomeRecord gene)
        {
            ID = gene.ID;
            Fitness = gene.Fitness;
            Generation = gene.Generation;
            Ofsprings = gene.Ofsprings;
        }

        public string ID { get; }
        public double Fitness { get; }
        public int Generation { get; }
        public int Ofsprings { get; }
    }

    internal sealed class PopulationMemberRow
    {
        internal PopulationMemberRow(ISmartObject member)
        {
            ID = member.ID;
            Generation = member.Generation;
            Fitness = member.Fitness;
            HP = member.HP;
            Cycles = member.Cycles;
            Ofsprings = member.Ofsprings;
        }

        public string ID { get; }
        public int Generation { get; }
        public double Fitness { get; }
        public double HP { get; }
        public int Cycles { get; }
        public int Ofsprings { get; }
    }
}
