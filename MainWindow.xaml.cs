using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using HelixToolkit.Wpf.SharpDX;
using System.Threading;

namespace PointCloudViewer
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    MainViewModel mViewModel;
    private CancellationTokenSource cts = new CancellationTokenSource();


    public MainWindow()
    {
      InitializeComponent();

      mViewModel = new MainViewModel(new SharpDX.Size2(670, 375));
      this.DataContext = mViewModel;

      var token = cts.Token;

      Task.Run(() =>
      {
        while (!token.IsCancellationRequested)
        {
          mViewModel.UpdatePC();
          UpdateInfo();
          Task.Delay(16).Wait();
        }
      }, token);


    }
    protected override void OnClosed(EventArgs e)
    {
      PointCloudViewer.Properties.Settings.Default.Save();
      base.OnClosed(e);
    }


    private void UpdateInfo()
    {
      Dispatcher.Invoke(new Action(() =>
      {
        fps.Content = String.Format("{0:0.0} ", mViewModel.FPS);
      }));
    }


    private void zoomOnPC_Click(object sender, RoutedEventArgs e)
    {
      mViewModel.Camera.ZoomExtents(viewport3D, mViewModel.PointCloud.BoundingSphere.Center.ToPoint3D(),
        mViewModel.PointCloud.BoundingSphere.Radius);
    }

    private void checkBox_Checked(object sender, RoutedEventArgs e)
    {
      mViewModel.fastUpdate = checkBox.IsChecked.Value;
    }
  }
}
