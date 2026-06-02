using System;
using System.ComponentModel;
using System.Windows.Forms;
using AI_Evlo_Test.Objects;
using ArtificialNeuralNetwork;

namespace AI_Evlo_Test
{
    public partial class VisualizeNetwork : Form

    {
        public delegate void VisualizerMessage_Handler(string Message);
        public event VisualizerMessage_Handler VisualizerSendMessage;

        public MainWindow ParentFormNN;
        INeuralNetwork VisualizedNNet;
        EvolutionChember evChamb = new EvolutionChember();
        /// <summary>
        /// Set or get status text displayed on the status bar at the bottom of the form
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Status
        {
            get { return statusStrip1.Items["toolStripStatusText"].Text; }
            set { statusStrip1.Items["toolStripStatusText"].Text = value; }
        }

        public VisualizeNetwork()
        {
            InitializeComponent();
            evChamb.NewMessage += EvChamb_NewMessage;
            if (Objects.WindowBoundsStore.TryGet("VisualizeNetwork", out double w, out double h))
                Size = new System.Drawing.Size((int)w, (int)h);
            FormClosing += (s, e) => Objects.WindowBoundsStore.Save("VisualizeNetwork", Width, Height);
        }

        private void EvChamb_NewMessage(string Message)
        {
            VisualizerSendMessage?.Invoke(Message);
        }

        internal void ShowNNet(INeuralNetwork nNetwork)
        {
            DrawVisualization(nNetwork);
        }

        
        private void DrawVisualization(INeuralNetwork nNetwork = null)
        {
            if (nNetwork == null)
                nNetwork = VisualizedNNet;
            else
                VisualizedNNet = nNetwork;
            if (nNetwork == null)
            {
                Status = "Network not selected.";
                networkView.Network = null;
                return;
            }

            networkView.Network = nNetwork;
            Status = "Neural network graph rendered.";
        }

        private void btnRefreshNNVisual_Click(object sender, EventArgs e)
        {
            DrawVisualization();
        }

        private void BtnMutate_Click(object sender, EventArgs e)
        {
            EvolutionChember evChamb = new EvolutionChember();

            VisualizedNNet = evChamb.MutateNN(VisualizedNNet, 5);
            DrawVisualization();

        }
        private void ChkAutoRefresh_CheckedChanged(object sender, EventArgs e)
        {
            timer1.Enabled = chkAutoRefresh.Checked;
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            DrawVisualization(this.VisualizedNNet);
        }

    }
}
