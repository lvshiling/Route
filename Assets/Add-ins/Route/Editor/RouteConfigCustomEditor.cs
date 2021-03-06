﻿/* Copyright 2018 xerysherry
 * 
 * Permission is hereby granted, free of charge, to any person 
 * obtaining a copy of this software and associated documentation 
 * files (the "Software"), to deal in the Software without restriction, 
 * including without limitation the rights to use, copy, modify, 
 * merge, publish, distribute, sublicense, and/or sell copies of 
 * the Software, and to permit persons to whom the Software is 
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall 
 * be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE 
 * FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR 
 * IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
 * IN THE SOFTWARE.
 */

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RouteConfig))]
public class RouteConfigDrawer : Editor
{
    [MenuItem("Route/Create Route Config")]
    static void Create()
    {
        GameObject obj = new GameObject("RouteConfig");
        var config = obj.AddComponent<RouteConfig>();
        EditorGUIUtility.PingObject(obj);
        Selection.objects = new GameObject[] { obj };
        SceneView.lastActiveSceneView.LookAt(obj.transform.position);

        var route = new GameObject("Route");
        var point = route.AddComponent<RoutePoint>();
        point.transform.parent = config.transform;
        point.transform.position = Vector3.left * 2;
        point.prev_weight_point = Vector3.left;
        point.next_weight_point = Vector3.right;

        route = new GameObject("Route");
        point = route.AddComponent<RoutePoint>();
        point.transform.parent = config.transform;
        point.transform.position = Vector3.right * 2;
        point.prev_weight_point = Vector3.left;
        point.next_weight_point = Vector3.right;

        config.Refresh();
    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DoGUI(0);
        base.OnInspectorGUI();
    }

    public void OnSceneGUI()
    {   
        GUI.Window(1, new Rect(5, 20, 250, 170), DoGUI, "Route Config(路径配置窗口)");
    }

    public static void Select(RoutePoint p)
    {
        var obj = p.gameObject;
        Selection.objects = new GameObject[] { obj };
        SceneView.currentDrawingSceneView.LookAt(obj.transform.position);
    }
    public static void Select(RouteConfig p)
    {
        var obj = p.gameObject;
        Selection.objects = new GameObject[] { obj };
        SceneView.currentDrawingSceneView.LookAt(obj.transform.position);
    }

    void DoGUI(int id)
    {
        var self = target as RouteConfig;
        if(GUILayout.Button("New Route Point(创建新路点)"))
            CreateNewPoint();
        if(GUILayout.Button("Refresh(刷新)"))
        {
            self.Refresh();
            SceneView.RepaintAll();
        }
        if(GUILayout.Button("Select Head Route(选择路径头)"))
        {
            if(self.points.Length > 0)
                Select(self.points[0]);
        }
        if(GUILayout.Button("Select Tail Route(选择路径尾)"))
        {
            if(self.points.Length > 0)
                Select(self.points[self.points.Length - 1]);
        }
        if(!self.IsLoop)
        {
            if(GUILayout.Button("Unloop(非闭环模式)"))
            {
                self.SetLoop(true);
                self.Refresh();
                SceneView.RepaintAll();
            }
        }
        else
        {
            if(GUILayout.Button("Loop(闭环模式)"))
            {
                self.SetLoop(false);
                self.Refresh();
                SceneView.RepaintAll();
            }
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("Total Length(总长度):");
        GUILayout.Label(self.total_length.ToString());
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Route Count(路点数):");
        GUILayout.Label(self.points.Length.ToString());
        GUILayout.EndHorizontal();
    }

    void CreateNewPoint()
    {
        RouteConfig config = target as RouteConfig;

        var obj = new GameObject("Route");
        obj.transform.parent = config.transform;
        var point = obj.AddComponent<RoutePoint>();

        if(config.points != null && config.points.Length > 0)
        {
            //设置最后一个节点的变换
            var lastpoint = config.points[config.points.Length - 1];
            var transform = config.points[config.points.Length - 1].transform;
            obj.transform.position = transform.position;
            obj.transform.rotation = transform.rotation;

            //point.message = lastpoint.message;
            //point.keeptime = lastpoint.keeptime;
            //point.velocity = lastpoint.velocity;
            point.prev_weight_point = lastpoint.prev_weight_point;
            point.next_weight_point = lastpoint.next_weight_point;
            point.color = lastpoint.color;
        }

        config.Refresh();

        EditorGUIUtility.PingObject(obj);
    }
}

[CustomEditor(typeof(RouteLine))]
public class RouteLineDrawer : Editor
{
    [MenuItem("Route/Extends/Create Route Line")]
    static void Create()
    {
        GameObject obj = new GameObject("RouteLine");
        obj.AddComponent<RouteLine>();
        EditorGUIUtility.PingObject(obj);
        Selection.objects = new GameObject[] { obj };
        SceneView.lastActiveSceneView.LookAt(obj.transform.position);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        if(GUILayout.Button("Build Mesh"))
            (target as RouteLine).Remesh();
        base.OnInspectorGUI();
    }
}

[CustomEditor(typeof(RoutePoint))]
public class RoutePointDrawer : Editor
{
    static readonly Color kRightArrowColor = new Color(1, 0.2f, 0.2f);
    static readonly Color kForwardArrowColor = new Color(0.2f, 0.2f, 1);
    static readonly Color kUpArrowColor = new Color(0.2f, 1, 0.2f);
    static readonly Color kDirectionArrowColor = new Color(1, 1, 0.2f);
    static bool show_arrow_ = false;

    public void OnSceneGUI()
    {
        RoutePoint point = target as RoutePoint;
        var velocity = point.velocity;
        if(velocity == 0)
        {
            var c = point.GetConfig();
            if(c != null)
                velocity = c.velocity;
        }
            
        Handles.color = Color.black;
        Handles.Label(point.transform.position,
            point.name + "\n" +
            "S:" + velocity + " " +
            "W:" + point.keeptime);

        //是否显示操作杆
        if(!show_arrow_)
        {
            GUI.Window(1, new Rect(5, 20, 200, 150), DoGUI, "Point Config(路点配置窗口)");
            return;
        }
        GUI.Window(1, new Rect(5, 20, 200, 210), DoGUI, "Point Config(路点配置窗口)");

        Quaternion rot = new Quaternion();
        Quaternion irot = new Quaternion();
        RouteConfig config = point.GetConfig();
        if(config)
        {
            rot = config.transform.rotation;
            irot = Quaternion.Inverse(rot);
        }

        var old_next_weight_point = point.transform.position + rot * point.next_weight_point;
        var next_weight_point = HandleCoordinate(old_next_weight_point, "next");
        var old_prev_weight_point = point.transform.position + rot * point.prev_weight_point;
        var prev_weight_point = HandleCoordinate(old_prev_weight_point, "prev");

        if(old_next_weight_point != next_weight_point)
        {
            point.next_weight_point = irot * (next_weight_point - point.transform.position);
            if(point.weight_type == RoutePoint.WeightType.kNormal)
                point.prev_weight_point = -point.next_weight_point.normalized * point.prev_weight_point.magnitude;
            old_next_weight_point = next_weight_point;
        }
        if(old_prev_weight_point != prev_weight_point)
        {
            point.prev_weight_point = irot * (prev_weight_point - point.transform.position);
            if(point.weight_type == RoutePoint.WeightType.kNormal)
                point.next_weight_point = -point.prev_weight_point.normalized * point.next_weight_point.magnitude;
            old_prev_weight_point = prev_weight_point;
        }

        var dir1 = rot * point.next_weight_point;
        next_weight_point = HandleDirection(old_next_weight_point, dir1.normalized);
        var dir2 = rot * point.prev_weight_point;
        prev_weight_point = HandleDirection(old_prev_weight_point, dir2.normalized);

        if(old_next_weight_point != next_weight_point)
        {
            point.next_weight_point = point.next_weight_point.normalized * 
                (next_weight_point - point.transform.position).magnitude;
        }
        if(old_prev_weight_point != prev_weight_point)
        {
            point.prev_weight_point = point.prev_weight_point.normalized *
                (prev_weight_point - point.transform.position).magnitude;
        }

        Handles.color = Color.red;
        Handles.DrawDottedLine(point.transform.position, next_weight_point, 10);
        Handles.DrawDottedLine(point.transform.position, prev_weight_point, 10);
    }

    Vector3 HandleCoordinate(Vector3 pos, string name)
    {
        var next = pos;
        var handlesize = HandleUtility.GetHandleSize(next);

#if UNITY_4_6
        Handles.color = kRightArrowColor;
        next = Handles.Slider(next, Vector3.right, handlesize, Handles.ArrowCap, 1);
        Handles.color = kForwardArrowColor;
        next = Handles.Slider(next, Vector3.forward, handlesize, Handles.ArrowCap, 1);
        Handles.color = kUpArrowColor;
        next = Handles.Slider(next, Vector3.up, handlesize, Handles.ArrowCap, 1);
#else // UNITY_5 || UNITY_2017
        Handles.color = kRightArrowColor;
        next = Handles.Slider(next, Vector3.right, handlesize, Handles.ArrowHandleCap, 1);
        Handles.color = kForwardArrowColor;
        next = Handles.Slider(next, Vector3.forward, handlesize, Handles.ArrowHandleCap, 1);
        Handles.color = kUpArrowColor;
        next = Handles.Slider(next, Vector3.up, handlesize, Handles.ArrowHandleCap, 1);
#endif
        Handles.Label(next, name);
        return next;
    }

    Vector3 HandleDirection(Vector3 pos, Vector3 dir)
    {
        if(dir == Vector3.zero)
            return Vector3.zero;

#if UNITY_4_6
        Handles.color = kDirectionArrowColor;
        return Handles.Slider(pos, dir, HandleUtility.GetHandleSize(pos), Handles.ArrowCap, 1);
#else // UNITY_5 || NITY_2017
        Handles.color = kDirectionArrowColor;
        return Handles.Slider(pos, dir, HandleUtility.GetHandleSize(pos), Handles.ArrowHandleCap, 1);
#endif
    }

    void DoGUI(int id)
    {
        RoutePoint point = target as RoutePoint;
        point.name = EditorGUILayout.TextField(point.name);

        if(GUILayout.Button("Ping(在节点树高亮)"))
            EditorGUIUtility.PingObject(point);
        if(GUILayout.Button("Back RouteConfig(返回路径配置)"))
        {
            var c = point.GetConfig();
            if(c != null)
                Selection.objects = new GameObject[] { c.gameObject };
        }
        if(GUILayout.Button("Prev Route(下一个路点)"))
        {
            var c = point.GetConfig();
            if(c != null)
            {
                int i = c.points.Length - 1;
                for(; i > 0; --i)
                {
                    if(c.points[i] == point)
                        break;
                }
                if(i > 0)
                    RouteConfigDrawer.Select(c.points[i - 1]);
            }
        }
        if(GUILayout.Button("Next Route(上一个路点)"))
        {
            var c = point.GetConfig();
            if(c != null)
            {
                int i = 0;
                for(; i < c.points.Length - 1; ++i)
                {
                    if(c.points[i] == point)
                        break;
                }
                if(i < c.points.Length - 1)
                    RouteConfigDrawer.Select(c.points[i + 1]);
            }
        }
        if(GUILayout.Button(show_arrow_ ? "Hide Arrow(隐藏曲线操作杆)"
                                        : "Show Arrow(显示曲线操作杆)"))
        {
            show_arrow_ = !show_arrow_;
            //point.ShowArrow(show_arrow_);
        }

        if(!show_arrow_)
            return;

        switch(point.weight_type)
        {
        case RoutePoint.WeightType.kNormal:
            if(GUILayout.Button("Normal(正常模式)"))
                point.weight_type = RoutePoint.WeightType.kBroken;
            break;
        case RoutePoint.WeightType.kBroken:
            if(GUILayout.Button("Broken(断开模式)"))
                point.weight_type = RoutePoint.WeightType.kNormal;
            break;
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("Prev");
        if(GUILayout.Button("x=0"))
            point.prev_weight_point.x = 0;
        if(GUILayout.Button("y=0"))
            point.prev_weight_point.y = 0;
        if(GUILayout.Button("z=0"))
            point.prev_weight_point.z = 0;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Next");
        if(GUILayout.Button("x=0"))
            point.next_weight_point.x = 0;
        if(GUILayout.Button("y=0"))
            point.next_weight_point.y = 0;
        if(GUILayout.Button("z=0"))
            point.next_weight_point.z = 0;
        GUILayout.EndHorizontal();
    }
}
