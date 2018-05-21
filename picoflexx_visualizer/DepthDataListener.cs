using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoyaleDotNet;
using System.Windows;

namespace cam_cs
{
    //delegate void showDataDelegate(DepthData data);
    public class DepthDataListener: IDepthDataListener
    {
        DepthData data;
        //showDataDelegate dlgt;
        MainWindow mainWindow;
        IAsyncResult ar;
        public delegate void UpdateImageCallback(DepthData data);
        public DepthDataListener(MainWindow window){
            this.mainWindow = window;
        }

        /*~DepthDataListener()
        {
            if (dlgt != null)
            {
                dlgt.EndInvoke(ar);
            }
        }*/

        public void OnNewData (DepthData data){
            this.data = deepCopyData(data);
            
            /*if (dlgt != null)
            {
                dlgt.EndInvoke(ar);
            }
            dlgt = new showDataDelegate(mainWindow.showData);
            ar = dlgt.BeginInvoke(this.data, null, null);*/
            mainWindow.imgDataGray.Dispatcher.Invoke(new UpdateImageCallback(mainWindow.UpdateImage), data);
        }

        private DepthData deepCopyData(DepthData data)
        {
            DepthData ret = new DepthData();
            ret.exposureTimes = new uint[data.exposureTimes.Length];
            Array.Copy(data.exposureTimes, ret.exposureTimes, data.exposureTimes.Length);
            ret.height = data.height;
            ret.width = data.width;
            ret.version = data.version;
            ret.timeStamp = data.timeStamp;
            ret.points = new DepthPoint[data.points.Length];
            int i=0;
            foreach (var x in data.points)
            {
                ret.points[i] = new DepthPoint();
                ret.points[i].x = x.x;
                ret.points[i].y = x.y;
                ret.points[i].z = x.z;
                ret.points[i].noise = x.noise;
                ret.points[i].grayValue = x.grayValue;
                ret.points[i].depthConfidence = x.depthConfidence;
                i++;
            }
            return ret;
        }
    }
}
