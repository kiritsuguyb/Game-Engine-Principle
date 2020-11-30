using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedCurveGenerator : MonoBehaviour
{
    [Header("五个用来绘制冲刺速度曲线的变量，请勿随意更改")]
    [Tooltip("这段时间内由加速产生的额外增加的路程总量")]
    [Range(0.1f, 200)]
    public float D = 50;
    [Tooltip("加速阶段，t1是第一段加速的时长,t1越大，加速持续越久，最大速度越慢，过程越平缓")]
    [Range(0.01f, 5)]
    public float t1 = 0.1f;
    [Tooltip("加速阶段，a是第一段加速时曲线增长的幂次，幂次越大加速初期越平缓，后期越陡峭，反之亦然")]
    [Range(0.1f,10)]
    public float a = 0.5f;
    [Tooltip("减速阶段，t2是整段速度变化的时长，t2越大，减速缓冲就越久，同样最大速度也会越慢，过程越平缓")]
    [Range(0.5f,10)]
    public float t2 = 1f;
    [Tooltip("减速阶段，b是第一段加速的幂次的倒数，b越大减速初期越平缓，后期越陡峭，反之亦然")]
    [Range(0.1f, 10)]
    public float b = 5;
    [HideInInspector]
    public float maxSpeed;
    private void Start()
    {
        Debug.Assert(t1 < t2,"t1 应该小于t2");
        maxSpeed= caculateVelocity(t1);
    }

    //计算当前冲刺速度

    public float caculateVelocity(float n)
    {
        //推导过的速度公式，会保证每帧算出的速度积分为一个定值
        float velocity = 0;
        if (n > 0 && n < t1)
        {
            velocity = Mathf.Pow((n / t1), a);
        }
        if (n >= t1 && n <= t2)
        {
            velocity = 1 - Mathf.Pow((n - t1) / (t2 - t1), 1 / b);
        }
        float c = t1 / (a + 1) + t2 - t1 - (t2 - t1) / (1 / b + 1);
        return velocity = velocity / c * D;
    }
}
