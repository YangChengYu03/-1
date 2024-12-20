using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataManagementTools;
using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.Geoprocessing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using ESRI.ArcGIS.DataSourcesOleDB;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Framework;

namespace 实习1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }


        /*1.打开数据库*/
        private void OpenGBD(string Path)
        {
            IWorkspace workspace;
            IWorkspaceFactory myWorkspaceFactory = new FileGDBWorkspaceFactory();//创建工作工厂
            workspace = myWorkspaceFactory.OpenFromFile(Path, 0);//打开数据库
            IEnumDataset myEnumDataset = workspace.Datasets[esriDatasetType.esriDTAny];//获取数据集
            IDataset myDataset;
            while ((myDataset = myEnumDataset.Next()) != null)
            {
                if (myDataset is IFeatureClass)
                {
                    IGeoFeatureLayer featureLayer = new FeatureLayerClass();
                    featureLayer.FeatureClass = myDataset as IFeatureClass;
                    comboBox1.Items.Add(myDataset.Name);
                    axMapControl1.AddLayer(featureLayer as ILayer, 0);
                }
            }//遍历数据集获取shp文件
          

            Marshal.ReleaseComObject(workspace);
            Marshal.ReleaseComObject(myEnumDataset);
        }
        /*2.点集table添加xy数据*/
        private void PointFeatureClassAddXY() {
            Geoprocessor GP = new Geoprocessor();//创建gp工具
            AddXY addxyTool = new AddXY();//调用添加xy工具

            addxyTool.in_features = path+$"\\{comboBox1.SelectedItem}";//输入
            addxyTool.out_features = path + $"\\{comboBox1.SelectedItem}";//输出
            try { GP.Execute(addxyTool, null); MessageBox.Show("cg"); }
            catch
            {
                for (int i = 0; i < GP.MessageCount; i++)
                {
                    MessageBox.Show(GP.GetMessage(i));//执行失败导出错误信息
                }
            }
            
        }
        /*3.导出点集csv表*/
        public void ExportFeatureClassToCSV() {
            IFeatureLayer myFeatureLayer = axMapControl1.get_Layer(comboBox1.Items.Count-comboBox1.SelectedIndex-1) as IFeatureLayer;//选择点集图层
            IFeatureClass myFeatureClass = myFeatureLayer.FeatureClass;
            using (StreamWriter Writer = new StreamWriter(path+".\\TemData.csv", false, Encoding.UTF8))//写入CSV文件，编码UFT-8
            {
                for (int i = 0; i < myFeatureClass.Fields.FieldCount; i++)
                {
                    Writer.Write(myFeatureClass.Fields.get_Field(i).Name);
                    if (i < myFeatureClass.Fields.FieldCount - 1)
                        Writer.Write(",");//CSV逗号分隔
                }
                Writer.WriteLine();//换行

                // 遍历要素并写入每一行数据
                IFeatureCursor featureCursor = myFeatureClass.Search(null, false);
                IFeature feature;
                while ((feature = featureCursor.NextFeature()) != null)
                {
                    for (int i = 0; i < myFeatureClass.Fields.FieldCount; i++)
                    {
                        Writer.Write(feature.get_Value(i).ToString());
                        if (i < myFeatureClass.Fields.FieldCount - 1)
                            Writer.Write(",");
                    }
                    Writer.WriteLine();
                }
            }
            Marshal.ReleaseComObject(myFeatureLayer);
            Marshal.ReleaseComObject(myFeatureClass);
        }
        /*4调用python文件，执行吸引力计算统计及表导出*/
        private void runPython() {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = @"E:\python3.11.3\python.exe"; // Python 可执行文件的路径
            start.Arguments = @"E:\GIS开发实习材料\实习1\tabelProcess.py"; // 传递 Python 脚本
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.CreateNoWindow = true;

            using (Process process = Process.Start(start))
            {
                process.WaitForExit();//等待执行完毕
            }
        }

        /*5执行点转线操作*/
        private void PointToLine() {
            string csvPath = Path.GetDirectoryName(path);
            string csvName = Path.GetFileName(csvPath + ".\\testtable_processed1.csv");//获取CSV路径
            IWorkspaceFactory myWorkspaceFactory = new OLEDBWorkspaceFactory();
            IPropertySet myPropSet = new PropertySet();
            myPropSet.SetProperty("CONNECTSTRING", "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + csvPath + ";Extended Properties='Text;HDR=Yes;IMEX=1;CharacterSet=65001;'");//CSV UTF-8编码转表设置
            IWorkspace myWorkspace = myWorkspaceFactory.Open(myPropSet, 0);
            IFeatureWorkspace myFeatureWorkspace2 = (IFeatureWorkspace)myWorkspace;
            ITable myTable = myFeatureWorkspace2.OpenTable(csvName);//打开Table表
            Geoprocessor GP = new Geoprocessor();
            XYToLine xyToLineTool = new XYToLine();//调用XY转线工具
            xyToLineTool.in_table = myTable;
            xyToLineTool.startx_field = "START_X";
            xyToLineTool.starty_field = "START_Y";
            xyToLineTool.endx_field = "END_X";
            xyToLineTool.endy_field = "END_Y";
            xyToLineTool.out_featureclass = path + ".\\xyToLine";
            xyToLineTool.spatial_reference = ((IGeoDataset)axMapControl1.get_Layer(0)).SpatialReference;
            try
            {
                GP.Execute(xyToLineTool, null);
                MessageBox.Show("2");
            }
            catch 
            {
                for (int i = 0; i < GP.MessageCount; i++)
                {
                    MessageBox.Show(GP.GetMessage(i));
                }
            }
        }

        /*6添加该shp，并创建样式*/
        private void AddResult() {
            IWorkspace workspace;
            IWorkspaceFactory myWorkspaceFactory = new FileGDBWorkspaceFactory();//创建工作工厂
            workspace = myWorkspaceFactory.OpenFromFile(path, 0);//打开数据库
            IEnumDataset myEnumDataset = workspace.Datasets[esriDatasetType.esriDTAny];//获取数据集
            IDataset myDataset;
            while ((myDataset = myEnumDataset.Next()) != null)
            {
                if (myDataset.Name=="xyToLine")
                {
                    IGeoFeatureLayer featureLayer = new FeatureLayerClass();
                    featureLayer.FeatureClass = myDataset as IFeatureClass;
                    axMapControl1.AddLayerFromFile(@"E:\GIS开发实习材料\实习1\style.lyr",0);//添加已经创好样式的图层
                    ILayer styleLayer;
                    for (int i = 0; i < axMapControl1.LayerCount; i++)
                    {
                        styleLayer = axMapControl1.get_Layer(i);
                        if (styleLayer.Name == "style")
                        {
                            featureLayer.Renderer = ((IGeoFeatureLayer)styleLayer).Renderer ;
                            axMapControl1.DeleteLayer(i);
                            axMapControl1.AddLayer(featureLayer);
                            axMapControl1.Refresh();
                        }
                    }
                    
                    comboBox1.Items.Add(myDataset.Name);
                    break;
                }
            }
            axMapControl1.Refresh();
            Marshal.ReleaseComObject(workspace);
            Marshal.ReleaseComObject(myEnumDataset);
        }

        string path;
        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog FBDL = new FolderBrowserDialog();//打开文件目录
            FBDL.ShowDialog();
            path = FBDL.SelectedPath;

            OpenGBD(path);
        }

        private void comboBox1_Click(object sender, EventArgs e)
        {
          
        }

        private void button2_Click(object sender, EventArgs e)
        {
            PointFeatureClassAddXY();
            ExportFeatureClassToCSV();
            runPython();
            PointToLine();
            AddResult();
        }
    }
}
