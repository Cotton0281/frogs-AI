using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AI_Evlo_Test.Objects;
using ArtificialNeuralNetwork;
using NeuralNetworkVisualizer;
using NeuralNetworkVisualizer.Model;
using NeuralNetworkVisualizer.Model.Layers;
using NeuralNetworkVisualizer.Model.Nodes;
using NeuralNetworkVisualizer.Preferences.Brushes;
using NeuralNetworkVisualizer.Preferences.Formatting;
using NeuralNetworkVisualizer.Preferences.Text;
using NeuralNetworkVisualizer.Selection;

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

            /******** Configure Some Preferences: ********/

            //Drawing resize behavior
            NeuralNetworkVisualizerControl1.Preferences.AsyncRedrawOnResize = false; //default is true

            //Font, Colors, etc.
            NeuralNetworkVisualizerControl1.Preferences.Inputs.OutputValueFormatter = new ByValueSignFormatter<TextPreference>(
                () => new TextPreference { Brush = new SolidBrushPreference(Color.Red) },
                () => new TextPreference { Brush = new SolidBrushPreference(Color.Gray) },
                () => new TextPreference { Brush = new SolidBrushPreference(Color.Black) },
                () => new TextPreference { Brush = new SolidBrushPreference(Color.Black) }
            );

            NeuralNetworkVisualizerControl1.Preferences.Perceptrons.OutputValueFormatter = new ByValueSignFormatter<TextPreference>(
                () => new TextPreference { Brush = new SolidBrushPreference(Color.Red) },
                () => new TextPreference { Brush = new SolidBrushPreference(Color.Gray) },
                () => new TextPreference { Brush = new SolidBrushPreference(Color.Black) },
                () => new TextPreference { Brush = new SolidBrushPreference(Color.Black) }
            );

            NeuralNetworkVisualizerControl1.Preferences.Edges.ValueFormatter = new ByValueSignFormatter<TextPreference>(
                () => new TextPreference { Brush = new SolidBrushPreference(Color.Red) },
                () => new TextPreference { Brush = new SolidBrushPreference(Color.Gray) },
                () => new TextPreference { Brush = new SolidBrushPreference(Color.Black) },
                () => new TextPreference { Brush = new SolidBrushPreference(Color.Black) }
            );

            NeuralNetworkVisualizerControl1.Preferences.Edges.Connector = new CustomFormatter<Pen>((v) => v == 0.0 ? new Pen(Color.LightGray) : new Pen(Color.Black));

            //Graphics quality
            NeuralNetworkVisualizerControl1.Preferences.Quality = RenderQuality.High; //Low, Medium, High. Medium is default

            //To remove layer titles
            //NeuralNetworkVisualizerControl1.Preferences.Layers = null;

            //** NOTE: ** Preferences setting don't redraw the control automatically. If you need to redraw the current rendered NN, call to Redraw() method after all setting 
            //NeuralNetworkVisualizerControl1.Redraw();



            /***** Some Functionalities *****/

            //Adjust zoom
            NeuralNetworkVisualizerControl1.Zoom = 2.0f; //1.0 is 'normal' and default, fit the whole drawing to control size

            //Get the current rendered NN to save to disk or whatever
            Image img = NeuralNetworkVisualizerControl1.Image;


            /*************** Make NN Elements Selectable *****************/
            //The selectable elements are: Layers, Nodes (all types) and Edge connectors.
            // Do a single click for single selection.
            // Press **SHIFT** key when click for multiple one.
            // Press **CTRL** key when click to unselect an element.

            NeuralNetworkVisualizerControl1.Selectable = true; //default is false

            //Each selectable element has its own typed-safe "Select" event
            NeuralNetworkVisualizerControl1.SelectBias += NeuralNetworkVisualizerControl1_SelectBias;
            NeuralNetworkVisualizerControl1.SelectEdge += NeuralNetworkVisualizerControl1_SelectEdge;
            NeuralNetworkVisualizerControl1.SelectInput += NeuralNetworkVisualizerControl1_SelectInput;
            NeuralNetworkVisualizerControl1.SelectInputLayer += NeuralNetworkVisualizerControl1_SelectInputLayer;
            NeuralNetworkVisualizerControl1.SelectPerceptron += NeuralNetworkVisualizerControl1_SelectPerceptron;
            NeuralNetworkVisualizerControl1.SelectPerceptronLayer += NeuralNetworkVisualizerControl1_SelectPerceptronLayer;
             

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
                return;
        }
            /*************** Set the NN Model *****************/


            var _input = new InputLayer("Input")
            {
                Bias = new Bias("bias") { OutputValue = 1 },
            };

            int intCount = 0;
            foreach (Synapse inpurNeuron in nNetwork.Inputs)
            {
                intCount++;
                _input.AddNode(new Input("In" + intCount) { OutputValue = inpurNeuron.Axon.Value });
            }

            LayerBase LastLayer = _input;
            intCount = 0;
            foreach (ILayer hidLayer in nNetwork.HiddenLayers)
            {
               
                int intCount2 = 0;
                var hidden = new PerceptronLayer("Hidden" + intCount);
                foreach (INeuron hidNeuron in hidLayer.NeuronsInLayer)
                {
                    Perceptron newPreceptron = new Perceptron("H" + intCount + "_" + intCount2)
                    {
                        ActivationFunction = ActivationFunction.Tanh,
                        OutputValue = hidNeuron.Axon.Value,
                        SumValue = hidNeuron.Soma.CalculateSummation(),
                         
                    };
                    hidden.AddNode(newPreceptron);
                    intCount2++;
                }
                LastLayer.Connect(hidden);
                LastLayer = hidden;
                intCount++;
            }

            intCount = 0;
            var output = new PerceptronLayer("Output");
            foreach (INeuron outNeuron in nNetwork.OutputLayer.NeuronsInLayer)
            {
                intCount++;
                output.AddNode(new Perceptron("O" + intCount)
                {
                    ActivationFunction = ActivationFunction.Tanh,
                    OutputValue = outNeuron.Axon.Value,
                    SumValue = outNeuron.Soma.CalculateSummation()
                });
            }

            LastLayer.Connect(output);
            NeuralNetworkVisualizerControl1.InputLayer = _input; //Automatic rendering
                                                                 //NeuralNetworkVisualizerControl1.InputLayer = null;
                                                                 //Leave blank when needed

            ////////
            ///
            List<PerceptronLayer> lsVisLayers = new List<PerceptronLayer>();
            PerceptronLayer visLayer = NeuralNetworkVisualizerControl1.InputLayer.Next;
            while (visLayer != null)
            {
                lsVisLayers.Add(visLayer);
                visLayer = visLayer.Next;
            }

            PerceptronLayer visualLayer;
            ILayer nnLayer;
            for(int i=0;i<lsVisLayers.Count;i++)
            {
                if (i < lsVisLayers.Count-1)
                    nnLayer = nNetwork.HiddenLayers[i];
                else
                    nnLayer = nNetwork.OutputLayer;

                visualLayer = lsVisLayers[i];
                List<Perceptron> visLayers = visualLayer.Nodes.ToList();
                for (int i2=0;i2< visLayers.Count();i2++)
                {
                    Perceptron visNeuron = visLayers[i2];
                    INeuron nnNeuron = nnLayer.NeuronsInLayer[i2];
                    List<Edge> lsEdges = visNeuron.Edges.ToList();
                    for (int i3 = 0; i3 < lsEdges.Count; i3++) // edge in p.Edges)
                    {
                        if (i3 == 0)
                            lsEdges[i3].Weight = nnNeuron.Soma.Bias;
                        else
                            lsEdges[i3].Weight = nnNeuron.Soma.Dendrites[i3-1].Weight;
                    }
                }
            }
            //foreach (var p in output.Nodes)
            //{
            //    foreach (var edge in p.Edges)
            //    {
            //        edge.Weight = aleatorio.NextDouble();
            //    }
            //}

           
        }

        private void NeuralNetworkVisualizerControl1_SelectPerceptron(object sender, SelectionEventArgs<Perceptron> e)
        {
            Status = e.Element.Id + ":" + e.Element.OutputValue;

        }

        private void NeuralNetworkVisualizerControl1_SelectPerceptronLayer(object sender, SelectionEventArgs<PerceptronLayer> e)
        {
        }

        private void NeuralNetworkVisualizerControl1_SelectInput(object sender, SelectionEventArgs<Input> e)
        {
            Status = e.Element.Id + ":" + e.Element.OutputValue;
        }

        private void NeuralNetworkVisualizerControl1_SelectInputLayer(object sender, SelectionEventArgs<InputLayer> e)
        {
        }

        private void NeuralNetworkVisualizerControl1_SelectEdge(object sender, SelectionEventArgs<Edge> e)
        {
            Status = "["+e.Element.Id + "]:" + e.Element.Weight;
        }

        private void NeuralNetworkVisualizerControl1_SelectBias(object sender, SelectionEventArgs<Bias> e)
        {
            Status = e.Element.Id + ":" + e.Element.OutputValue;
        }

        private void btnRefreshNNVisual_Click(object sender, EventArgs e)
        {
            DrawVisualization();
        }

        private void BtnMutate_Click(object sender, EventArgs e)
        {
            EvolutionChember evChamb = new EvolutionChember();

            evChamb.MutateNN(VisualizedNNet, 5);
            DrawVisualization();
            // NeuralNetworkVisualizerControl1.Redraw();

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
