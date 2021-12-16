// See https://aka.ms/new-console-template for more information

using System.ComponentModel;
using System.Drawing;
using Winleafs.Api;
using Winleafs.Api.Endpoints;
using Winleafs.Models.Enums;
using Winleafs.Server;

WinleafsServerClient client = new WinleafsServerClient();
//Dictionary<string, string> playLists = client.SpotifyEndpoint.GetPlaylists();

NanoleafClient nanoleafClient = new NanoleafClient("192.168.68.104", 16021, "nE3BXM1pNjBEvciPBdY1pfdk9jswTa2h");
var layout = nanoleafClient.LayoutEndpoint.GetLayout();

ExternalControlEndpoint externalControlEndpoint = new ExternalControlEndpoint(nanoleafClient);
await externalControlEndpoint.PrepareForExternalControl(DeviceType.Aurora, null);

int[] panelIds = layout.PanelPositions.Select(position => position.PanelId).ToArray();

List<Color> colors = new List<Color>(new []{Color.Red, Color.Blue, Color.Chartreuse, Color.AntiqueWhite});
List<Color> violetColors = new List<Color>(new []{Color.BlueViolet, Color.BlueViolet, Color.BlueViolet, Color.BlueViolet });

List<int> panelIds1 = new List<int> { panelIds[0], panelIds[1], panelIds[2], panelIds[3] };
List<int> panelIds2 = new List<int> { panelIds[4], panelIds[5], panelIds[6], panelIds[7] };
List<int> panelIds3 = new List<int> { panelIds[8], panelIds[9], panelIds[10], panelIds[11] };

var violetDict = GetVioletDict(0, 12);

externalControlEndpoint.SetPanelsColors(DeviceType.Aurora, violetDict.Keys.ToList(), violetDict.Values.ToList());
externalControlEndpoint.SetPanelsColors(DeviceType.Aurora, violetDict.Keys.ToList(), violetDict.Values.ToList());
externalControlEndpoint.SetPanelsColors(DeviceType.Aurora, violetDict.Keys.ToList(), violetDict.Values.ToList());


for (int i = 0; i < 10; i++)
{
    Thread.Sleep(100);

    externalControlEndpoint.SetPanelsColors(DeviceType.Aurora, panelIds1, colors);
    externalControlEndpoint.SetPanelsColors(DeviceType.Aurora, panelIds2, violetColors);
    externalControlEndpoint.SetPanelsColors(DeviceType.Aurora, panelIds3, violetColors);

    Thread.Sleep(100);

    externalControlEndpoint.SetPanelsColors(DeviceType.Aurora, panelIds1, violetColors);
    externalControlEndpoint.SetPanelsColors(DeviceType.Aurora, panelIds2, colors);
    externalControlEndpoint.SetPanelsColors(DeviceType.Aurora, panelIds3, violetColors);

    Thread.Sleep(100);

    externalControlEndpoint.SetPanelsColors(DeviceType.Aurora, panelIds1, violetColors);
    externalControlEndpoint.SetPanelsColors(DeviceType.Aurora, panelIds2, violetColors);
    externalControlEndpoint.SetPanelsColors(DeviceType.Aurora, panelIds3, colors);
}

Console.ReadKey();

Dictionary<int, Color> GetVioletDict(int startAt = 0, int colorCount = 4)
{
    Dictionary<int, Color> dict = new Dictionary<int, Color>();
    for (int i = startAt; i < colorCount + startAt; i++)
    {
        dict.Add(panelIds[i], Color.BlueViolet);
    }

    return dict;
}

Dictionary<int, Color> GetColorDict(int startAt = 0, params Color[] colors)
{
    Dictionary<int, Color> dict = new Dictionary<int, Color>();
    for (int i = startAt; i < colors.Length + startAt; i++)
    {
        dict.Add(panelIds[i], colors[i - startAt]);
    }

    return dict;
}
