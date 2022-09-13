using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows;


namespace UR_Installation_Tool
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    //private List<string> installationRead;
    private List<string> installationNew;
    private List<string> tcpFile;
    private List<string> featureFile;

    private MyInstallation myInstallation;

    public MainWindow()
    {
      InitializeComponent();
      ListOfTcp = new ObservableCollection<MyTcp>();
      ListOfPayloads = new ObservableCollection<MyPayload>();
      ListOfFeatures = new ObservableCollection<MyPointFeature>();
      this.DataContext = this;
    }

    public ObservableCollection<MyTcp> ListOfTcp { get; set; }
    public ObservableCollection<MyPayload> ListOfPayloads { get; set; }
    public ObservableCollection<MyPointFeature> ListOfFeatures { get; set; }
    public bool IsNewVersion { get; set; }
    


    private void Btn_Save_Installation(object sender, RoutedEventArgs e)
    {
      SaveNewInstallation();
    }

    private void Btn_Load_Tcp(object sender, RoutedEventArgs e)
    {
      CommonOpenFileDialog dialog = new CommonOpenFileDialog();

      if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
      {
        
        tcpFile = new List<string>(File.ReadAllLines(dialog.FileName));
        AnalyzeFile(tcpFile);
      }
    }

    private void Btn_Load_Features(object sender, RoutedEventArgs e)
    {
      CommonOpenFileDialog dialog = new CommonOpenFileDialog();

      if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
      {

        featureFile = new List<string>(File.ReadAllLines(dialog.FileName));
        AnalyzeFile(featureFile);
      }
    }

    private void Btn_Load_Installation(object sender, RoutedEventArgs e)
    {
      CommonOpenFileDialog dialog = new CommonOpenFileDialog();

      if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
      {
        List<string> installationRead;
        string path;
        string buffer = "";
        List<string> result;

        using (FileStream stream = new FileStream(dialog.FileName, FileMode.Open))
        {
          using (GZipStream gZipStream = new GZipStream(stream, CompressionMode.Decompress))
          {
            using(MemoryStream memoryStream = new MemoryStream())
            {
              gZipStream.CopyTo(memoryStream);
              buffer += Encoding.ASCII.GetString(memoryStream.ToArray());
              result = buffer.Split('\n').ToList();
            }
          }
        }

        installationRead = result;
        path = dialog.FileName;
        InstallationOld.ItemsSource = installationRead;
        myInstallation = new MyInstallation("unknown", installationRead, path);
        myInstallation.AnalyzeInstallation();
      }
    }

    private void SaveNewInstallation()
    {
      string fileName = "meine";
      SaveFileDialog dialog = new SaveFileDialog();
      dialog.DefaultExt = "installation";
      dialog.FileName = $"{fileName}.installation";
      dialog.CheckPathExists = true;

      if (dialog.ShowDialog() == true)
      {
        string saveString = "";
        foreach(string line in installationNew)
        {
          saveString = saveString + line + "\n";
        }
        
        var buffer = Encoding.ASCII.GetBytes(saveString);       
        using (var stream = new MemoryStream())
        {
          using (FileStream file = new FileStream(dialog.FileName,
              FileMode.Create, FileAccess.Write))
          {
            using (GZipStream gzs = new GZipStream(file, CompressionLevel.Fastest))
            {
              gzs.Write(buffer, 0, buffer.Length);
            }            
          }
        }
      }
    }
    private void Btn_Build_Installation(object sender, RoutedEventArgs e)
    {
        BuildNewInstallation();
    }

    private void AnalyzeFile(List<string> file)
    {
      foreach (string line in file)
      {
        if (line.StartsWith("TOOL "))
        {
          AddTcpToList(line);
        }
        else if (line.StartsWith("TOOL_LOAD"))
        {
          AddPayloadToList(line);
        }
        else if (line.Contains("="))
        {
          AddFeatureToList(line);
        }
      }
    }
    private void AddTcpToList(string line)
    {
      string leadingName = line.Substring(5);
      string tcpName = leadingName.Remove(leadingName.IndexOf(" ")); // tcp_torch
      string tcpValuesRaw = leadingName.Substring(leadingName.IndexOf("= ") + 2);// -0.0011743523,-0.0000539968,0.4664277801,1.8668300659,1.8754825639,0.6341750422
      string[] arrayOfTcpValues = tcpValuesRaw.Split(',');

      string tcpValues = "";
      int i = 0;
      foreach (string value in arrayOfTcpValues)
      {
        tcpValues += RoundValue(Convert.ToDouble(value, CultureInfo.CreateSpecificCulture("en-US")));
        if (i < 5)
        {
          tcpValues += ", ";
        }
        i++;
      }
      string uniqueIdExtension = ListOfTcp.Count().ToString("X6");
      string id = "f26012b1-504a-47a2-b7d1-46d1cc" + uniqueIdExtension;
      ListOfTcp.Add(new MyTcp(tcpName, tcpValues, id));
    }

    private void AddPayloadToList(string line)
    {
      string leadingName = line.Substring(10);
      string payloadName = leadingName.Remove(leadingName.IndexOf(" ")); // tcp_torch
      string payloadValuesRaw = leadingName.Substring(leadingName.IndexOf("= ") + 2);// 3.5,0.022,-0.011,0.092
      string[] arrayOfPayloadValues = payloadValuesRaw.Split(',');

      string payloadValues = "";
      string payloadWeight = "";
      int i = 0;
      foreach (string value in arrayOfPayloadValues)
      {
        if(i == 0)
        {
          payloadWeight = RoundValue(Convert.ToDouble(value, CultureInfo.CreateSpecificCulture("en-US")));
        }
        else
        {
          payloadValues += RoundValue(Convert.ToDouble(value, CultureInfo.CreateSpecificCulture("en-US")));
          if (i < 3)
          {
            payloadValues += ", ";
          }
        }        
        i++;
      }
      string inertiaMatrix = ConvertInertiaToMatrix(CalculateInertia(payloadWeight));
      ListOfPayloads.Add(new MyPayload(payloadName, payloadValues, payloadWeight, inertiaMatrix));
    }

    private void AddFeatureToList(string line)
    {
      string featureName = line.Remove(line.IndexOf(" ")); //grp_right
      string featureValuesRaw = line.Substring(line.IndexOf("= ") + 2);
      string[] arrayOfFeatureValues = featureValuesRaw.Split(';');

      string featureValuesXYZ = "";
      string featureValuesRXRYRZ = "";
      int i = 0;
      foreach(string valueRaw in arrayOfFeatureValues)
      {
        string value = valueRaw.Replace(",", ".");
        if(value == "")
        {
          value = "0.0";
        }
        if(i<3)
        {
          double val = (Convert.ToDouble(value, CultureInfo.CreateSpecificCulture("en-US"))) / 1000.0;
          
          CultureInfo culture = CultureInfo.InvariantCulture;
          featureValuesXYZ += val.ToString("G0", culture);
          if (i<2)
          {
            featureValuesXYZ += ", ";
          }
        }
        else
        {
          double val = Convert.ToDouble(value, CultureInfo.CreateSpecificCulture("en-US"));
          CultureInfo culture = CultureInfo.InvariantCulture;
          featureValuesRXRYRZ += val.ToString("G0", culture);
          if(i<5)
          {
            featureValuesRXRYRZ += ", ";
          }
        }
        i++;
      }
      string uniqueIdExtension = ListOfFeatures.Count().ToString("X6");
      string id = "f26012b1-504a-47a2-b7d1-46d1cc" + uniqueIdExtension;
      ListOfFeatures.Add(new MyPointFeature(featureName, featureValuesXYZ, featureValuesRXRYRZ, id));
    }

    private void BuildNewInstallation()
    {
      installationNew = myInstallation.Content;
      if (ListOfPayloads.Count is not 0)
      {
        AddNewPayloads(myInstallation.PayloadIndex);
      }
      if (ListOfFeatures.Count is not 0)
      {
        AddNewFeature(myInstallation.FeatureIndex);
      }
      if (ListOfTcp.Count is not 0)
      {
        AddNewTcp(myInstallation.TcpIndex);
      }

      InstallationNew.ItemsSource = installationNew;
    }
      private void AddNewTcp(int index)
    {
      //Build Line
      int i = index;
      foreach (MyTcp tcp in ListOfTcp)
      {
        string newTcp = @$"      <tcp id=""{tcp.Id}"" name=""{tcp.Name}"" offset=""{tcp.Values}""/>";
        installationNew.Insert(index, newTcp);
        i++;
      }
    }
    private void AddNewPayloads(int index)
    {
      //Build Line
      int i = index;
      foreach (MyPayload payload in ListOfPayloads)
      //    <Payload name="Payload" mass="5.0" defaultPayload="false" centerOfGravity="0.0, 0.0, 0.0" inertiaParameters="0.022505, 0.022505, 0.022505, 0.0, 0.0, 0.0" customInertiaEnabled="false"/>
      {
        string newPayload = @$"    <Payload name=""{payload.Name}"" mass=""{payload.Weight}"" defaultPayload=""false"" centerOfGravity=""{payload.Values}"" inertiaParameters=""{payload.Inertia}"" customInertiaEnabled=""false""/>";
        installationNew.Insert(i, newPayload);
        i++;
      }
    }
    private void AddNewFeature(int index)
    {
      //Build Line
      int i = index;


      foreach (MyPointFeature feature in ListOfFeatures)
      {
        string line = "";
        installationNew.Insert(i, "    <GeomPoseNode>"); //      <GeomPoseNode showAxes="true" joggable="true" isVariable="true" id="8173f4ee-1154-4192-b8a2-c114c1ca23a1" name="Point_1">
        i++;
        line = @$"      <GeomPoseNode showAxes=""true"" joggable=""true"" isVariable=""true"" id=""{feature.Id}"" name=""{feature.Name}"">";
        installationNew.Insert(i, line);
        i++;
        installationNew.Insert(i, @"        <jointPositionVector value=""0.0, 0.0, 0.0, 0.0, 0.0, 0.0""/>");
        i++;
        line = @$"        <toolPosition value=""{feature.ValuesXYZ}""/>";
        installationNew.Insert(i, line);
        i++;
        line = @$"        <toolAxisAngle value=""{feature.ValuesRXRYRZ}""/>";
        installationNew.Insert(i, line);
        i++;
        installationNew.Insert(i, @"        <tcpOffset value=""0.0, 0.0, 0.0, 0.0, 0.0, 0.0""/>");
        i++;
        installationNew.Insert(i, @"      </GeomPoseNode>");
        i++;
        installationNew.Insert(i, @"    </GeomPoseNode>");
        i++;
      }
    }

    private string ConvertInertiaToMatrix(double inertia)
    {
      
      CultureInfo culture = CultureInfo.InvariantCulture;
      string inertiaString = inertia.ToString("F6", culture);
      string inertiaMatrix = $"{inertiaString}, {inertiaString}, {inertiaString}, 0.0, 0.0, 0.0";
      return inertiaMatrix;
    }

    private double CalculateInertia(string weight)
    {
      double mass = Convert.ToDouble(weight, CultureInfo.CreateSpecificCulture("en-US"));
      double volume = mass / 1000; // m / 1000[kg/m³]
      double r = Math.Pow(((3 * volume) / 4 * Math.PI), (1.0/1.3)); // (3*V/4*Pi)^(1/3)
      double j = 2.0 / 5.0 * mass * r * r;
      
      return Math.Round(j, 6); ;
    }

    private string RoundValue(double number)
    {
      CultureInfo culture = CultureInfo.InvariantCulture;
      return Math.Round(number, 4).ToString("N", culture);
    }


  }

  public class MyPointFeature
  {
    public string Name { get; set; }
    public string ValuesXYZ { get; set; }
    public string ValuesRXRYRZ { get; set; }
    public string Id { get; set; }

    public MyPointFeature(string name, string valuesXYZ, string valuesRXRYRZ, string id)
    {
      Name = name;
      ValuesXYZ = valuesXYZ;
      ValuesRXRYRZ = valuesRXRYRZ;
      Id = id;
    }
  }

  public class MyTcp
  {
    public string Name { get; set; }
    public string Values { get; set; }
    public string Id { get; set; }

    public MyTcp(string name, string values, string id)
    {
      Name = name;
      Values = values;
      Id = id;
    }
  }
  public class MyPayload
  {
    public string Name { get; set; }
    public string Weight { get; set; }
    public string Inertia { get; set; }
    public string Values { get; set; }


    public MyPayload(string name, string values, string weight, string inertia)
    {
      Name = name;
      Values = values;
      Weight = weight;
      Inertia = inertia;
    }
  }
  public class MyInstallation
  {
    public string Name { get; set; }
    public List<string> Content { get; set; }
    public string Path { get; set; }
    public int TcpIndex { get; set; }
    public int PayloadIndex { get; set; }
    public int FeatureIndex { get; set; }

    public MyInstallation(string name, List<string> content, string path, int tcpIndex = 0, int payloadIndex = 0, int featureIndex = 0)
    {
      Name = name;
      Content = content;
      Path = path;
      TcpIndex = tcpIndex;
      PayloadIndex = payloadIndex;
      FeatureIndex = featureIndex;
    }

    public void AnalyzeInstallation()
    {
      int i = 0;
      foreach (string line in Content)
      {
        if (CheckForTcpLine(line))
        {
          TcpIndex = i + 3;
        }
        if (CheckForPayloadLine(line))
        {
          PayloadIndex = i + 2;
        }
        if (CheckForFeatureLine(line))
        {
          FeatureIndex = i + 1;
        }
        i ++;
      }
    }

    private bool CheckForTcpLine(string line)
    {
      if (line.Contains("<TCPSettings"))
      {
        return true;
      }
      else
      {
        return false;
      }
    }

    private bool CheckForPayloadLine(string line)
    {
      if (line.Contains("<PayloadSettings"))
      { 
        return true;
      }
      else
      {
        return false;
      }
    }

    private bool CheckForFeatureLine(string line)
    {
      if (line.Contains("</ToolView>"))
      {
        return true;
      }
      else
      {
        return false;
      }
    }

  }
}
