using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PointCloudViewer
{
  using HelixToolkit.Wpf.SharpDX;
  using SharpDX;
  using System;
  using System.Threading;
  using Point3D = System.Windows.Media.Media3D.Point3D;
  using Transform3D = System.Windows.Media.Media3D.Transform3D;
  using TranslateTransform3D = System.Windows.Media.Media3D.TranslateTransform3D;
  using RotateTransform3D = System.Windows.Media.Media3D.RotateTransform3D;
  using Vector3D = System.Windows.Media.Media3D.Vector3D;
  using System.Windows.Media.Media3D;

  public class MainViewModel : BaseViewModel
  {

    #region Scene objects
    private SynchronizationContext context = SynchronizationContext.Current;
    /// <summary>
    /// The Grid
    /// </summary>
    public LineGeometry3D GridMajor { get; private set; }
    /// <summary>
    /// The Grid
    /// </summary>
    public LineGeometry3D GridMinor { get; private set; }
    /// <summary>
    /// The Grid
    /// </summary>
    public LineGeometry3D GridLongRange { get; private set; }
    /// <summary>
    /// The Grid
    /// </summary>
    public LineGeometry3D GridLongRangeMajor { get; private set; }
    /// <summary>
    /// View x axis
    /// </summary>
    public LineGeometry3D XAxis { get; private set; }
    /// <summary>
    /// View y axis
    /// </summary>
    public LineGeometry3D YAxis { get; private set; }
    /// <summary>
    /// View z axis
    /// </summary>
    public LineGeometry3D ZAxis { get; private set; }

    public HelixToolkit.Wpf.SharpDX.MeshGeometry3D Origin { get; private set; }
    public PhongMaterial OriginMaterial { get; private set; }

    public PointGeometry3D PointCloud { get; private set; }
    /// <summary>
    /// Thickness of the Lines of the Grid
    /// </summary>
    public double LineThickness { get; set; }
    /// <summary>
    /// Thickness of the Lines of the Triangulated Polygon
    /// </summary>
    public double TriangulationThickness { get; set; }
    /// <summary>
    /// Color of the Gridlines
    /// </summary>
    public System.Windows.Media.Color GridColor { get; private set; }
    /// <summary>
    /// Transform of the Grid
    /// </summary>
    public Transform3D GridTransform { get; private set; }
    /// <summary>
    /// Transform of the Grid
    /// </summary>
    public Transform3D GridLRTransform { get; private set; }
    /// <summary>
    /// Transform of the Grid
    /// </summary>
    public Transform3D PCTransform { get; private set; }
    /// <summary>
    /// Direction of the directional Light
    /// </summary>
    public Vector3 DirectionalLightDirection { get; private set; }
    /// <summary>
    /// Color of the directional Light
    /// </summary>
    public Color4 DirectionalLightColor { get; private set; }
    /// <summary>
    /// Color of the ambient Light
    /// </summary>
    public System.Windows.Media.Color AmbientLightColor { get; private set; }
    /// <summary>
    /// Draw the Triangles or not
    /// </summary>
    private Boolean mShowTriangleLines;
    /// <summary>
    /// Accessor to the Boolean
    /// </summary>
    public Boolean ShowTriangleLines
    {
      get
      {
        return mShowTriangleLines;
      }
      set
      {
        mShowTriangleLines = value;
        OnPropertyChanged("ShowTriangleLines");
      }
    }
    /// <summary>
    /// The Polygon-Material
    /// </summary>
    private PhongMaterial mMaterial;
    /// <summary>
    /// Accessor to the Polygon-Material
    /// </summary>
    public PhongMaterial Material
    {
      get
      {
        return mMaterial;
      }
      set
      {
        mMaterial = value;
        OnPropertyChanged("Material");
      }
    }

    #endregion

    /// <summary>
    /// PC Size
    /// </summary>
    /// <param name="newSize"></param>
    public void SetSize(System.Drawing.Size newSize)
    {
      this.Size = new Size2(newSize.Width, newSize.Height);
      InitPointCloud(Color4Extensions.FromArgb(0, 255, 0));

    }

    public Size2 Size { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public int PointCount { get { return Size.Width * Size.Height; } }

    private DateTime phase = DateTime.Now;
    public double FPS { get; private set; }
    double a = 0;


    /// <summary>
    /// Constructor of the MainViewModel
    /// Sets up allProperties
    /// </summary>
    public MainViewModel(Size2 pcSize)
    {
      Size = pcSize;
      // Render Setup
      EffectsManager = new DefaultEffectsManager();
      RenderTechnique = EffectsManager[DefaultRenderTechniqueNames.Blinn];

      // Window Setup
      this.Title = "Point Cloud";
      this.SubTitle = null;

      // Camera Setup
      this.Camera = new HelixToolkit.Wpf.SharpDX.PerspectiveCamera
      {
        Position = new Point3D(0, 3, -3),
        LookDirection = new Vector3D(0, -1, 4),
        UpDirection = new Vector3D(0, 1, 0)
      };
      this.Camera.LookAt(new Point3D(0, 0, 0), 0);

      // Lines Setup
      this.LineThickness = 1;
      this.TriangulationThickness = .5;
      this.ShowTriangleLines = true;

      // Lighting Setup
      this.AmbientLightColor = System.Windows.Media.Colors.White;
      this.DirectionalLightColor = Color.White;
      this.DirectionalLightDirection = new Vector3(0, -1, 0);

      // Model Materials and Colors
      this.Material = PhongMaterials.PolishedBronze;


      // Grid Setup
      int GRID_SIZE = 10;
      int GRID_LONG_RANGE_SIZE = 40;
      var gridTransform = new Transform3DGroup();
      gridTransform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 90)));
      gridTransform.Children.Add(new TranslateTransform3D(-GRID_SIZE / 2, -0.01, -GRID_SIZE / 2));
      GridTransform = gridTransform;

      // Major
      LineBuilder lb = new LineBuilder();
      lb.AddGrid(BoxFaces.Bottom, 11, 11, GRID_SIZE, GRID_SIZE);
      this.GridMajor = lb.ToLineGeometry3D();
      this.GridColor = System.Windows.Media.Color.FromArgb(15, 200, 200, 200);

      // Minor
      lb = new LineBuilder();
      lb.AddGrid(BoxFaces.Bottom, 101, 101, GRID_SIZE, GRID_SIZE);
      this.GridMinor = lb.ToLineGeometry3D();

      // Long range
      lb = new LineBuilder();
      lb.AddGrid(BoxFaces.Bottom, GRID_LONG_RANGE_SIZE + 1, GRID_LONG_RANGE_SIZE + 1, GRID_LONG_RANGE_SIZE, GRID_LONG_RANGE_SIZE);
      this.GridLongRange = lb.ToLineGeometry3D();
      GridLRTransform = new TranslateTransform3D(-(GRID_LONG_RANGE_SIZE - GRID_SIZE) / 2, -(GRID_LONG_RANGE_SIZE - GRID_SIZE) / 2, 0);

      // Long range major
      lb = new LineBuilder();
      lb.AddGrid(BoxFaces.Bottom, GRID_LONG_RANGE_SIZE / 5 + 1, GRID_LONG_RANGE_SIZE / 5 + 1, GRID_LONG_RANGE_SIZE, GRID_LONG_RANGE_SIZE);
      this.GridLongRangeMajor = lb.ToLineGeometry3D();


      lb = new LineBuilder();
      lb.AddLine(new Vector3(0, 0, 0), new Vector3(GRID_SIZE / 2, 0, 0));
      this.XAxis = lb.ToLineGeometry3D();

      lb = new LineBuilder();
      lb.AddLine(new Vector3(0, 0, 0), new Vector3(0, GRID_SIZE / 2, 0));
      this.YAxis = lb.ToLineGeometry3D();

      lb = new LineBuilder();
      lb.AddLine(new Vector3(0, 0, 0), new Vector3(0, 0, GRID_SIZE / 2));
      this.ZAxis = lb.ToLineGeometry3D();

      var mb = new MeshBuilder();
      mb.AddSphere(new Vector3(0, 0, 0), 0.05);
      this.Origin = mb.ToMeshGeometry3D();

      OriginMaterial = new PhongMaterial()
      {
        AmbientColor = System.Windows.Media.Colors.Gray.ToColor4(),
        DiffuseColor = System.Windows.Media.Colors.Yellow.ToColor4(),
        SpecularColor = System.Windows.Media.Colors.White.ToColor4(),
        SpecularShininess = 100f,
      };




      PointCloud = new PointGeometry3D();
      PointCloud.IsDynamic = true;
      InitPointCloud(Color4Extensions.FromArgb(255, 0, 0));

    }

    /// <summary>
    /// Call each time PC size changes
    /// </summary>
    /// <param name="initColor"></param>
    void InitPointCloud(Color initColor)
    {
      var positions = new HelixToolkit.Wpf.SharpDX.Core.Vector3Collection();
      var colors = new HelixToolkit.Wpf.SharpDX.Core.Color4Collection();
      var indices = new HelixToolkit.Wpf.SharpDX.Core.IntCollection();
      for (int y = 0; y < Size.Height; y++)
        for (int x = 0; x < Size.Width; x++)
        {
          indices.Add(positions.Count);
          float mp = 0.01f;
          positions.Add(new Vector3(mp * x, mp * y, 0.0f));
          colors.Add(initColor);
        }
      context.Send((o) =>
      {
        PointCloud.Positions = positions;
        PointCloud.Colors = colors;
        PointCloud.Indices = indices;
      }, null);


    }

    public bool fastUpdate = false;


    /// <summary>
    /// Call on each frame
    /// </summary>
    public void UpdatePC()
    {

      var lastUpdate = DateTime.Now;
      a = (lastUpdate - phase).TotalMilliseconds / 5000.0;

      HelixToolkit.Wpf.SharpDX.Core.Vector3Collection positions;
      if (fastUpdate)
        positions = PointCloud.Positions;
      else positions = new HelixToolkit.Wpf.SharpDX.Core.Vector3Collection(PointCount);
      var colors = new HelixToolkit.Wpf.SharpDX.Core.Color4Collection(PointCount);
      float f = 1915; // px, focal length (fx')
      float B = 0.25f; // m, baseline B = Tx / (-fx') 
      int w = Size.Width, h = Size.Height;
      for (int y = 0, i = 0; y < h; y++)
        for (int x = 0; x < w; x++, i++)
        {
          {
            int px = x * 3 + y * w * 3;
            float yy = 0.01f * y, xx = 0.01f * x;
            var color = Color4Extensions.FromArgb((int)(255 * Math.Sin(yy+a)), (int)(255 * Math.Cos(xx+a)), 0);
            int pxd = x * 3 + y * w * 3;
            short disp = (short)(10 * Math.Sin(yy + a) * Math.Cos(xx + a));
            //short disp = (short)(100 +  10 * Math.Sin(yy + a) * Math.Cos(xx + a));
            float Z = f * B / (disp);
            //Z = 200;
            float u = w / 2 - x;
            float v = h / 2 - y;
            float X = u * Z / f;
            float Y = v * Z / f;
            if (fastUpdate)
              positions[i] = (new Vector3(X, Y, Z));
            else positions.Add(new Vector3(X, Y, Z));
            colors.Add(color);

          }
        }
      context.Send((o) =>
      {
        PointCloud.Positions = positions;
        PointCloud.Colors = colors;

        int d = 10;
        this.Camera.Position = new Point3D(Math.Cos(a) * d, 3, Math.Sin(a) * d);
        this.Camera.LookDirection = new Vector3D(-this.Camera.Position.X, -this.Camera.Position.Y, -this.Camera.Position.Z);
      }, null);

      var currentTime = DateTime.Now;
      var duration = currentTime - lastUpdate;
      FPS = 1000.0 / duration.TotalMilliseconds;
      lastUpdate = currentTime;

    }

  }
}
