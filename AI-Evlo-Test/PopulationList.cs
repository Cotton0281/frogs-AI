using AI_Evlo_Test.Objects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AI_Evlo_Test
{
    public partial class PopulationList : Form
    {
        Population objPopulaion = null;
        public PopulationList()
        {
            InitializeComponent();
        }
        public void SetDataSource(Population population)
        {
            objPopulaion = population;
            if (objPopulaion.Members == null)
            {
                throw new NullReferenceException("Population Members cannot be null.");
            }
            dataGridView1.DataSource = objPopulaion.lsBestGenes?.ToList();
            dataGridView2.DataSource = objPopulaion.Members;
        }

        public void RefreshList()
        {
            dataGridView1.DataSource = objPopulaion.lsBestGenes.OrderByDescending(o => o.Fitness).ToList();
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
            var r = 5;
        }
    }
}
