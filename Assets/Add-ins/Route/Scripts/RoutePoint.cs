﻿using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
[AddComponentMenu("CameraRoute/RoutePoint")]
public class RoutePoint : MonoBehaviour
{
    static readonly float kDistanceLine = 1.5f;

#if UNITY_EDITOR
    void Update()
    {
        transform.eulerAngles = Vector3.zero;
        UpdateColorSet();
    }

    void UpdateColorSet()
    {
        if(colorset == ColorSet.kNone)
            return;
        color = color_array[(int)colorset];
        colorset = ColorSet.kNone;
    }

    void OnDrawGizmosSelected()
    {
        OnDrawGizmos();
    }

    void OnDrawGizmos()
    {
        DrawPoint(color);
        DrawArrow(color);
    }

    void DrawPoint(Color color)
    {
        float r = transform.localScale.x * 0.5f;
        Gizmos.color = color;
        Gizmos.DrawSphere(transform.position, r);
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, r);
    }

    void DrawArrow(Color color)
    {
        if(next_weight_point == Vector3.zero)
            return;

        Gizmos.color = color;

        Quaternion rot = new Quaternion();
        RouteConfig config = GetConfig();
        if(config)
            rot = config.transform.rotation;

        //Vector3 direction = new Vector3(kDistanceLine, 0, 0);
        //direction = rot * direction;

        //var oldmx = Gizmos.matrix;
        ////Gizmos.matrix = Gizmos.matrix * Matrix4x4.TRS(
        ////    transform.position + rot * next_weight_point.normalized * kDistanceLine * transform.localScale.x,  
        ////    rot * Quaternion.LookRotation(next_weight_point) * Quaternion.AngleAxis(0, new Vector3(0, 1, 0)),
        ////    transform.localScale);
        //Gizmos.DrawFrustum(transform.position, 15, 0, 1, 1.2f);
        //Gizmos.matrix = oldmx;

        Gizmos.DrawRay(transform.position + Vector3.zero, rot * next_weight_point.normalized*10);
    }

    static readonly Color[] color_array = new Color[]
    {
        Color.white,
        Color.green,
        Color.red,
        Color.blue,
        Color.black,
        Color.cyan,
        Color.gray,
        Color.white,
        Color.magenta,
        Color.yellow,
    };
#endif

    void OnDestroy()
    {}

    public RouteConfig GetConfig()
    {
        if(!transform.gameObject)
            return GetComponent<RouteConfig>();

        GameObject curr = gameObject;
        RouteConfig config = null;
        do
        {
            var parent = curr.transform.parent.gameObject;
            config = parent.GetComponent<RouteConfig>();
            if(config)
                break;
            curr = parent;
        } while(curr.transform.parent != null);
        return config;
    }

    /// <summary>
    /// 颜色
    /// </summary>
    public Color color = Color.green;
    /// <summary>
    /// 快速设置
    /// </summary>
    public enum ColorSet
    { 
        kNone = 0,
        kGreen,
        kRed,
        kBlue,
        kBlack,
        kCyan,
        kGray,
        kWhite,
        kMegenta,
        kYellow,
    }
    public ColorSet colorset;
    /// <summary>
    /// 消息
    /// </summary>
    public string message = "";
    /// <summary>
    /// 停留时间
    /// </summary>
    public float keeptime = 0;
    /// <summary>
    /// 局部速度，前往下一点速度，0表示无效
    /// </summary>
    public float velocity = 0;

    public Vector3 prev_weight_point = new Vector3(-1, 0, 0);
    public Vector3 next_weight_point = new Vector3(1, 0, 0);

    public Vector3 delta_next { get { return next_weight_point - transform.position; } }
    public Vector3 delta_prev { get { return prev_weight_point - transform.position; } }

    public enum WeightType
    {
        kNormal,
        kBroken,
    }
    [HideInInspector]
    public WeightType weight_type_ = WeightType.kNormal;

    [HideInInspector]
    [SerializeField]
    public float[] steps_;
}