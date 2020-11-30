using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirdController : MonoBehaviour
{
    [Header("初始化")]
    public Vector3 InitPosition = Vector3.zero;
    public Vector3 InitForward = Vector3.forward;
    [Header("模型")]
    [Tooltip("鸟的模型，这里需要一个模型主要是因为需要控制这个模型的动画")]
    public GameObject birdModel;
    /// <summary>
    /// 决定是否要颠倒Y轴的输入，因为有些玩家习惯按上键往下飞
    /// </summary>
    bool reverseY =false;
    public bool reverseAxisY {
        get { return reverseY; }
        set { reverseY = value; }
    }
    public bool reverseYInput = false;
    [Header("飞行速度相关")]
    [Tooltip("鸟的飞行速度")]
    [Range(0.01f, 100)]
    public float flySpeed = 20;
    [Tooltip("鸟的左右转向速度")]
    [Range(0.01f, 100)]
    public float yawSpeed = 5;
    [Tooltip("鸟的抬头低头转向速度")]
    [Range(0.01f,100)]
    public float pitchSpeed = 2;

    [Header("Pitch转角相关")]
    [Tooltip("Y方向上的最大转角，0为最小，180最大。举例而言90意味着向上可以飞45度，向下也可以飞45度")]
    [Range(1f, 179)]
    public float yMaxAngle = 90;
    [Tooltip("Y方向上的最大转角偏移，举例而言，90度的最大转角加上10度的偏移，意味着向上最多飞35，向下最多飞55")]
    [Range(1f, 20f)]
    public float yDownwardOffset = 10;
    [Tooltip("Y方向上鸟的回正速度")]
    [Range(1f, 15f)]
    public float yRetriveSpeed = 5;

    /// <summary>
    /// 一些关于按键加速的控制标志
    /// lastYstate是上一帧Y轴上输入的状态，有输入为true，主要是为了由于区分上升沿和下降沿
    /// startAcceleration在上升沿会被设置为true，然后在将Accelerating设置为true后立刻把自身设置为false
    /// keepHighSpeed是在按住下键时保持最高速度用的
    /// accelerationTimeline是一个局部的时间变量，用于计算每次开始加速后经过的时间
    /// </summary>
    bool lastYstate =false;
    bool startAcceleration = false;
    bool Accelerating = false;
    bool keepHighSpeed = false;
    float accelerationTimeline = 0;

    /// <summary>
    /// 用来控制没有输入的时候动画的随机播放
    /// </summary>
    //一个标志位
    bool flytrriger = false;
    // wingTime是一个局部的时间变量，用于计算每次随机扇动翅膀后经过的时间
    float wingTime = 0;
    [Header("随机扇动翅膀")]
    [Tooltip("Idle飞行时两次扇动翅膀的最小间隔")]
    [Range(0.01f, 5f)]
    public float minWingTime=1;
    [Tooltip("Idle飞行时两次扇动翅膀的最大间隔")]
    [Range(1f, 10f)]
    public float maxWingTime = 5;
    [HideInInspector]
    public float cameraLerpFactor = 0;

    // Start is called before the first frame update
    private void OnEnable()
    {
        reverseAxisY = reverseYInput;
        transform.position = InitPosition;
        transform.forward = InitForward;
        GetComponent<Rigidbody>().velocity = transform.forward * flySpeed;
        GetComponent<Rigidbody>().useGravity = false;

    }
    void Start()
    {
        //物理材质的初始化，只要一次就够了
        PhysicMaterial zeroFriction = new PhysicMaterial();
        zeroFriction.name = "Totally Smooth Material";
        zeroFriction.bounciness = 0;
        zeroFriction.dynamicFriction = 0;
        zeroFriction.staticFriction = 0;
        zeroFriction.frictionCombine = PhysicMaterialCombine.Minimum;
        foreach (Collider collider in GetComponentsInChildren<Collider>())
        {
            collider.material = zeroFriction;
        }
    }

    // Update is called once per frame
    //TODO：
    ////1.有y输入的时候要改变方向，没有输入的时候要把上下方向拉正！！重要，这点好了会解放一定的操控
    ////2.转向的时候up向量要有一定程度歪曲，不一定非要朝着某个“不动点”，只要有个示意就会好很多
    //3.动画在没有输入的时候偶尔动一下就好了
    ////3.1.还要加速
    //4.加速的时候飞行要镜头畸变
    ////5.处理碰撞问题,这个问题实际是摩擦力问题，一个小球撞上某点肯定会转动，凑巧这个操控又是会向某个方向前进，自然就怼上去了。
    ////解决方案就是把物体的摩擦力属性归零
    //6.镜头焦距变化最好和速度变化脱钩
    //7.速度变化最好和WASD操控脱钩
    //处理Y的输入，包括上升沿下降沿更改加速状态和Y方向自动拉正
    private void FixedUpdate()
    {
        //这部分用于不定时地扇动翅膀
        wingTime += Time.deltaTime;
        float rN = Random.Range(0.0F, 1.0F);
        //Debug.Log(rN+"    wing time:"+wingTime);
        if (rN > 0.98 && wingTime > minWingTime)
        {
            flytrriger = true;
            wingTime = 0;
        }
        if (wingTime > maxWingTime)
        {
            flytrriger = true;
            wingTime = 0;
        }
        if (flytrriger) flytrriger = false;
    }
    void Update()
    {
        //首先处理输入
        float y=Input.GetAxis("Vertical");
        if (reverseAxisY) y = -y;
        float x=Input.GetAxis("Horizontal");

        //关联动画
        if (birdModel != null)
        {
            birdModel.GetComponent<Animator>().SetBool("flytrigger", flytrriger);
            birdModel.GetComponent<Animator>().SetFloat("x", x);
            birdModel.GetComponent<Animator>().SetFloat("y", y);
        }


        //如果y没输入且是下降沿，那就不能保持高速
        //Y没输入的时候要回正
        if (y < 0.001 && y > -0.001)
        {
            if (lastYstate == true) keepHighSpeed = false;
            lastYstate = false;
            //回正
            Vector3 flatforward = transform.forward - Vector3.Dot(Vector3.up, transform.forward) * Vector3.up;//获取y方向上为0的正方向
            transform.forward = Vector3.Lerp(transform.forward, flatforward, Time.deltaTime* yRetriveSpeed);//lerp
        }
        //按住下键时向下俯冲保持高速
        else if (y <= -0.99)
        {
            keepHighSpeed = true;
        }
        //如果Y有输入，且上次是false，说明是上升沿，要开始加速，更改标志位
        else
        {
            if (lastYstate == false) startAcceleration = true;
            lastYstate = true;
        }
        //限制原本y的角度
        if (!checkPitch(transform.forward))
        {
            transform.forward = transform.forward - Vector3.Dot(Vector3.up, transform.forward) * Vector3.up;
        }

        //先获得初始方向，再分别处理y和x
        Vector3 forword = transform.forward;
        var dir = forword;
        //如果y有输入，那那就跟着输入走
        Vector3 pitch = y * transform.up * pitchSpeed * Time.deltaTime;
        //限制输入y的角度，如果没问题，那就加上y的偏向
        if (checkPitch(forword+pitch))
        {
            dir = pitch + dir;
        }


        //处理x的输入
        //x,y的输入关联动画
        Vector3 yaw = x * transform.right * yawSpeed * Time.deltaTime;
        Vector3 upVec = Vector3.up;
        dir = yaw + dir;

        //前进方向单位化
        dir = dir.normalized;

        //改变速度
        float extraSpeed = 0;
        //开始加速，启用加速标志，禁用开始加速标志
        if (startAcceleration)
        {
            startAcceleration = false;
            accelerationTimeline = 0;
            Accelerating = true;
        }
        //如果有速度曲线组件且正在加速则进行加速操作
        if (Accelerating && null != GetComponent<SpeedCurveGenerator>())
        {
            //如果目前加速度很大且要保持，那就**不要**更改当前时间,让速度保持住
            if (!(keepHighSpeed && accelerationTimeline >= GetComponent<SpeedCurveGenerator>().t1)) accelerationTimeline += Time.deltaTime;
            else accelerationTimeline = GetComponent<SpeedCurveGenerator>().t1;
            //超过最大加速时间则加速结束
            if (accelerationTimeline > GetComponent<SpeedCurveGenerator>().t2)
            {
                accelerationTimeline = 0;
                Accelerating = false;
            }
            //获取当前加速阶段下的额外速度
            extraSpeed = GetComponent<SpeedCurveGenerator>().caculateVelocity(accelerationTimeline);
            cameraLerpFactor = extraSpeed / GetComponent<SpeedCurveGenerator>().maxSpeed;
        }

        //真正更改鸟飞行的方向
        transform.rotation = Quaternion.LookRotation(dir);
        //真正处理rigidbody的相关参数
        //更改鸟的速度
        GetComponent<Rigidbody>().velocity = dir * (flySpeed + extraSpeed);
        //角速度要压死，不然会冲突
        GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        GetComponent<Rigidbody>().inertiaTensorRotation = Quaternion.identity;
        GetComponent<Rigidbody>().ResetInertiaTensor();

    }

    bool checkPitch(Vector3 _dir)
    {
        float _pitch = Quaternion.LookRotation(_dir).eulerAngles.x;
        float _pitchnormal= (_pitch + 90) % 360;
        float forwardPitch = 90 + yDownwardOffset;
        return (_pitchnormal > forwardPitch - yMaxAngle / 2 && _pitchnormal < forwardPitch + yMaxAngle / 2);
    }

    private void OnGUI()
    {
        //    GUI.Label(new Rect(Vector2.zero, Vector2.one * 300), "lastYstate=\t" + lastYstate +"\n"+
        //"startAcceleration=\t" + startAcceleration + "\n"+
        //    "Accelerating=\t" + Accelerating + "\n"+
        //    "keepHighSpeed =\t" + keepHighSpeed+"\n"+
        //    "Velocity=\t"+GetComponent<Rigidbody>().velocity.magnitude+"\n"+
        //    "cameraLerpFactor=\t"+cameraLerpFactor);
        GUI.contentColor= Color.black;
        GUI.Label(new Rect(Vector2.one * 50, Vector2.one * 300), "Hold WASD to controll \n Press \"Space\" to reverse the Y Input \n Press \"F\" to switch between different camera follow mode");
        
    }
}
