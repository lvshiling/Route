﻿using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Bezier3D = RouteMath.Bezier3D;

[ExecuteInEditMode]
[AddComponentMenu("CameraRoute/RouteConfig")]
public class RouteConfig : MonoBehaviour
{
    /// <summary>
    /// 切割精细度
    /// 值越大切割就越精确，但是生成成本大（非运行时计算）
    /// </summary>
    static readonly float kStepParts = 100;

	void Awake () 
    {
        Refresh();
	}

#if UNITY_EDITOR
    //void Update () 
    //{
    ////禁止移动，旋转，缩放
    //transform.position = Vector3.zero;
    //transform.eulerAngles = Vector3.zero;
    //transform.localScale = Vector3.one;
    //}

    void DrawCurve(Bezier3D bezier, Quaternion rot, 
                RoutePoint pt0, RoutePoint pt1)
    {
        //var pt0 = points_[i];
        //var pt1 = points_[i + 1];

        var p0 = pt0.transform.position;
        var p1 = p0 + rot * pt0.next_weight_point;
        var p3 = pt1.transform.position;
        var p2 = p3 + rot * pt1.prev_weight_point;

        bezier.Set(p0, p1, p2, p3);

        float iv = 1 / ((p0 - p3).magnitude / (step_length * 2));
        if(iv < 0.001f)
            //间隔太大
            iv = 0.001f;

        Gizmos.color = pt0.color;
        if((pt0.delta_next.magnitude == 0 && pt0.delta_next.magnitude == 0))
        {
            Gizmos.DrawLine(p0, p3);
        }
        else
        {
            int arrow = 0;
            float t = 0;
            Vector3 lp = p0;
            Color dc = pt1.color - pt0.color;
            do
            {
                t += iv;
                if(t > 1.0f)
                    t = 1.0f;
                Vector3 p = bezier.Get(t);
                Gizmos.color = pt0.color + dc * t;
                Gizmos.DrawLine(lp, p);

                if(arrow++ % 2 == 0)
                {
                    Vector3 tangent = bezier.GetTangent(t);
                    var angle = RouteMath.GetPolarEular(tangent);
                    var quat = Quaternion.AngleAxis(angle, RouteMath.kAxisY);
                    Vector3 v0 = quat * RouteMath.kAxisX;
                    Vector3 axis = Vector3.Cross(p - lp, v0);
                    v0 *= 0.3f;

                    Vector3 v1 = Quaternion.AngleAxis(-70, axis) * v0;
                    Gizmos.DrawLine(p, p - v1);
                    v1 = Quaternion.AngleAxis(-110, axis) * v0;
                    Gizmos.DrawLine(p, p - v1);
                }

                lp = p;
            } while(t < 1.0f);
        }
    }

    void OnDrawGizmos() 
    {
        foreach(var point in points_)
        {
            if(!point)
            {
                Refresh();
                break;
            }
        }

        //绘制节点信息
        int count = points_.Length;
        if(count < 0)
            return;

        Gizmos.color = Color.green;
        Bezier3D bezier = new Bezier3D();
        Quaternion rot = transform.rotation;

        if(count > 1)
        {
            int i = 0;
            for(; i < count - 1; ++i)
            {
                DrawCurve(bezier, rot, points_[i], points_[i + 1]);
            }
            if(loop)
                DrawCurve(bezier, rot, points_[i], points_[0]);
        }
    }

    float RefreshLength(Bezier3D bezier, Quaternion rot, RoutePoint pt0, RoutePoint pt1)
    {
        var length = 0.0f;
        var p0 = pt0.transform.position;
        var p1 = p0 + rot * pt0.next_weight_point;
        var p3 = pt1.transform.position;
        var p2 = p3 + rot * pt1.prev_weight_point;

        bezier.Set(p0, p1, p2, p3);

        float step = 0;
        pt0.steps_ = new float[0];

        float t = 0;
        Vector3 lp = p0;

        do
        {
            t += microstep;
            if(t > 1.0f)
                t = 1.0f;
            Vector3 p = bezier.Get(t);
            var l = (lp - p).magnitude;
            step += l;
            if(step >= step_length)
            {
                ArrayUtility.Add(ref pt0.steps_, t);
                step -= step_length;
            }
            length += l;
            lp = p;
        } while(t < 1.0f);

        if(pt0.velocity > 0 && length / pt0.velocity < 0.001f)
            pt0.steps_ = new float[0];

        return length;
    }

    /// <summary>
    /// 刷新路径长度
    /// </summary>
    public void RefreshLength()
    {
        if(points.Length == 0)
            return;

        if(loop)
            length_ = new float[points_.Length];
        else
            length_ = new float[points_.Length - 1];
        total_length_ = 0;

        Bezier3D bezier = new Bezier3D();
        Quaternion rot = transform.rotation;

        int i = 0;
        for(; i < points_.Length - 1; ++i)
        {
            length_[i] = RefreshLength(bezier, rot, points_[i], points_[i + 1]);
            total_length_ += length_[i];
        }
        if(loop)
        {
            length_[i] = RefreshLength(bezier, rot, points_[i], points_[0]);
            total_length_ += length_[i];
        }
    }
#endif

    public void Refresh()
    {
        //获得路点信息
        points_ = GetComponentsInChildren<RoutePoint>();

#if UNITY_EDITOR
        RefreshLength();
#endif
    }

    public RoutePoint[] points { get { return points_; } }
    RoutePoint[] points_;

    public RoutePoint this[int index] 
    { 
        get
        {
            if(points_ == null || index >= points_.Length)
                return null;
            return points_[index];
        }
    }

    /// <summary>
    /// 切割单位长
    /// 值越小，切割越细，曲线越光滑，但是存储空间的越大
    /// </summary>
    [Range(0, 1)]
    public float step_length = 0.1f;
    /// <summary>
    /// 全局速度，在子节点速度未配置时有效。如果为0，表示无效
    /// </summary>
    public float velocity;
    /// <summary>
    /// 是否为闭环
    /// </summary>
    [HideInInspector]
    [SerializeField]
    public bool loop = false;
    /// <summary>
    /// 路径间长度缓存数据
    /// </summary>
    [HideInInspector]
    [SerializeField]
    public float[] length_;
    /// <summary>
    /// 总长度
    /// </summary>
    [HideInInspector]
    [SerializeField]
    public float total_length_;

    /// <summary>
    /// 通过总长度设置对应点，并获得标准量
    /// </summary>
    /// <param name="dist"></param>
    /// <returns></returns>
    public float GetNormalizedByTotalLength(float dist)
    {
        if(dist < 0)
            dist = 0;
        else if(dist > total_length_)
            dist = total_length_;

        int count = length_.Length;
        int curr = 0;
        float movelength = 0;
        for(; curr < count; ++curr)
        {
            var l = length_[curr];
            if(dist < l)
            {
                movelength = dist;
                break;
            }
            dist -= l;
        }
        SetCurrent(curr);
        return GetNormalizedT(movelength);
    }

    /// <summary>
    /// 通过总长度获得坐标点
    /// </summary>
    /// <param name="dist"></param>
    /// <returns></returns>
    public Vector3 GetPointByTotalLength(float dist)
    {
        return GetPoint(GetNormalizedByTotalLength(dist));
    }
    /// <summary>
    /// 通过总进程获得坐标点
    /// </summary>
    /// <param name="current_length"></param>
    /// <returns></returns>
    public Vector3 GetPointByTotalPrecent(float precent)
    {
        return GetPointByTotalLength(precent * total_length_);
    }

    /// <summary>
    /// 设置现在所在点
    /// </summary>
    public void SetCurrent(int current)
    {
        if(current < 0)
            current = 0;
        if(loop)
        {
            if(current > points_.Length - 1)
                current = points_.Length - 1;
        }
        else
        {
            if(current > points_.Length - 2)
                current = points_.Length - 2;
        }

        if(current_ == current)
            return;
        current_ = current;

        Quaternion rot = transform.rotation;
        RoutePoint pt0 = null, pt1 = null;
        if(current_ < points_.Length - 1)
        {
            pt0 = points_[current_];
            pt1 = points_[current_ + 1];
        }
        else
        {
            pt0 = points_[current_];
            pt1 = points_[0];
        }

        var p0 = pt0.transform.position;
        var p1 = p0 + rot * pt0.next_weight_point;
        var p3 = pt1.transform.position;
        var p2 = p3 + rot * pt1.prev_weight_point;
        bezier_.Set(p0, p1, p2, p3);
    }
    /// <summary>
    /// 设置现在下一个所在点
    /// </summary>
    public void Next()
    {
        if(loop)
            SetCurrent((current + 1) % points_.Length);
        else
            SetCurrent(current + 1);
    }
    /// <summary>
    /// 设置现在上一个所在点
    /// </summary>
    public void Prev()
    {
        if(loop)
            SetCurrent((current - 1 + points_.Length) % points_.Length);
        else
            SetCurrent(current - 1);
    }
    /// <summary>
    /// 当前所在点是否为头
    /// </summary>
    /// <returns></returns>
    public bool IsHead()
    {
        var l = points_.Length;
        if(l == 0)
            return false;
        else if(l == 1)
            return true;
        return current == 0;
    }
    /// <summary>
    /// 当前所在点是否为尾
    /// </summary>
    /// <returns></returns>
    public bool IsTail()
    {
        var l = points_.Length;
        if(l == 0)
            return false;
        else if(l == 1)
            return true;
        else if(loop)
            return current == l - 1;
        else
            return current == l - 2;
    }

    /// <summary>
    /// 获得标准T
    /// </summary>
    /// <param name="move">移动距离[0, current_length]</param>
    /// <returns></returns>
    public float GetNormalizedT(float move)
    {
        if(current_ >= points_.Length ||
            current_ >= length_.Length)
            return 0;
        if(current_point.steps_.Length == 0)
        {
            if(current_length == 0)
                return 0;
            return move / current_length;
        }
        var sf = (move / step_length);
        var si = (int)sf;
        if(si >= current_point.steps_.Length)
            return 1;

        float prevt = 0;
        if(si > 0)
            prevt = current_point.steps_[si - 1];
        float nextt = current_point.steps_[si];
        

        var t = (sf - si) * (nextt - prevt) + prevt;
        return t;
    }
    
    /// <summary>
    /// 获得曲线中一点
    /// </summary>
    /// <param name="t">百分比[0, 1]</param>
    /// <returns></returns>
    public Vector3 GetPoint(float t)
    {
        return bezier_.Get(t);
    }
    /// <summary>
    /// 获得曲线切线标准向量
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public Vector3 GetTangent(float t)
    {
        return bezier_.GetTangent(t);
    }

    public Vector3 GetPointByLength(float l)
    {
        return GetPoint(GetNormalizedT(l));
    }

    public Vector3 GetTangentByLength(float l)
    {
        return GetTangent(GetNormalizedT(l));
    }

    /// <summary>
    /// 当前点
    /// </summary>
    public RoutePoint current_point 
    { 
        get 
        {
            if(current_ < 0 || current_ > points_.Length - 1)
                return null;
            return points_[current_]; 
        } 
    }

    public RoutePoint next_point
    {
        get
        {
            if(current_ + 1 < 0 || current_ + 1 > points_.Length - 1)
                return null;
            return points_[current_ + 1];
        }
    }

    public float current_length 
    {
        get 
        {
            if(current_ < 0 || current_ >= length_.Length)
                return 0;
            return length_[current_]; 
        } 
    }
    /// <summary>
    /// 当前索引
    /// </summary>
    public int current { get { return current_; } }
    int current_ = -1;
    /// <summary>
    /// 路点数量
    /// </summary>
    public int count 
    { 
        get
        {
            if(points_ == null)
                return 0;
            return points_.Length;
        }
    }

    float microstep { get { return step_length / kStepParts; } }

    Bezier3D bezier_ = new Bezier3D();
    
}