using System.Linq;
using UnityEngine;

namespace XLScrapApi.Models;

public struct XLSimpleMainItem(XLMainItem xlMain)
{
    public Vector3 Position { get; set; }
        = xlMain.transform.position;
    public Vector3[] Anchors { get; set; }
        = xlMain.Anchors.ToArray();
    public Vector3[] HolderPositions { get; set; }
        = xlMain.HolderItems == null
            ? null
            : xlMain.HolderItems.Select(x => x.transform.position).ToArray();
}
