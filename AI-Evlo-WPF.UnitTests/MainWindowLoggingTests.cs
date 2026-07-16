using AI_Evlo_Test;
using System.Reflection;
using System.Threading;
using System.Windows.Threading;

namespace AI_Evlo_WPF.UnitTests;

[STATestClass]
public class MainWindowLoggingTests
{
    [TestMethod]
    public void Log_WhenCalledFromSimulationThread_QueuesMessageOnUiDispatcher()
    {
        var window = new MainWindow();
        try
        {
            MethodInfo? log = typeof(MainWindow).GetMethod(
                "Log",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(log);

            Exception? workerError = null;
            var worker = new Thread(() =>
            {
                try
                {
                    log.Invoke(window, new object[] { "background simulation message" });
                }
                catch (TargetInvocationException ex)
                {
                    workerError = ex.InnerException ?? ex;
                }
                catch (Exception ex)
                {
                    workerError = ex;
                }
            });

            worker.Start();
            Assert.IsTrue(worker.Join(TimeSpan.FromSeconds(2)), "The background Log call did not return.");

            var frame = new DispatcherFrame();
            window.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);

            Assert.IsNull(workerError, workerError?.ToString());
            FieldInfo? logField = typeof(MainWindow).GetField(
                "txtLog",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(logField);
            var textBox = (System.Windows.Controls.TextBox)logField.GetValue(window)!;
            StringAssert.Contains(textBox.Text, "background simulation message");
        }
        finally
        {
            window.Close();
        }
    }
}
