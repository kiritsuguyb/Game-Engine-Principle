using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class CameraController : MonoBehaviour
{
    [Header("初始化")]
    public Vector3 InitLookDir = Vector3.forward;
    //Camera directions
    //theta and phi stands for the spherical coodinates
    //getters and setters insure that the value of theta and phi wont get out af range
    //相机转角的两个核心参数，各有一个getter和setter保证输入正确
    float _theta;
    float _phi;
    public float thetaInDegree
    {
        get { return _theta; }
        set
        {
            _theta = asureThetaRange(value);
        }
    }
    public float phiInDegree
    {
        get { return _phi; }
        set
        {
            _phi = asurePhiRange(value);
        }
    }
    //camera controlling
    bool _CameraSmoothFollow = false;
    public bool CameraSmoothFollow {//相机是否采取平滑跟随模式
        get { return _CameraSmoothFollow; }
        set{_CameraSmoothFollow = value;}
    }
    public bool smoothFollow = false;
    //相机的指定距离，这个值就不暴露出去了因为最后会用两个projection vector控制
    float _cameraDist = 1;
    public float cameraDist {
        get { return _cameraDist; }
        private set { _cameraDist = value; }
    }
    [Header("虚拟相机，一定要有一个啊")]
    [Tooltip("Cinemachine相机")]
    public Cinemachine.CinemachineVirtualCamera virtualCamera;


    [Header("相机跟随")]
    [Tooltip("相机跟随鼠标的灵敏度")]
    [Range(0.2f,10f)]
    public float cameraSensitivity = 1;
    [Tooltip("相机角度转动时的平滑过渡速度")]
    [Range(0.5f, 5f)]
    public float cameraFollowSpeed = 1;


    [Header("相机焦距和相机距离")]
    [Tooltip("相机希区柯克变焦速度")]
    [Range(0.5f, 5f)]
    //x参数放的是fov,y参数放的是 camera Dist
    public float projectionChangeSpeed = 2;
    [Tooltip("普通速度下的fov和相机距离")]
    public Vector2 normalProjection =new Vector2(45, 10);
    [Tooltip("高速下的fov和相机距离")]
    public Vector2 highSpeedProjection = new Vector2(75, 5);


    Vector3 lookDir;
    Vector3 targetLookDir;
    float tiltAngle =60.0f;

    private void OnEnable()
    {
        CameraSmoothFollow = smoothFollow;
        // tuning cursor to stay only inside the screen
        //鼠标的可见性
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        //initailize
        var InitThetaPhi = LookDirection2ThetaPhi(InitLookDir);//这个函数会自动帮你计算出反方向的相机位置的thetaphi
        phiInDegree = InitThetaPhi.y;
        thetaInDegree = InitThetaPhi.x;
        virtualCamera.Follow = new GameObject().transform;
        virtualCamera.Follow.name="VirtualCameraFollowObject";
        if (CameraSmoothFollow)
        {
            //相机在镜头方向的反方向位置
            lookDir = -InitLookDir;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
    }
    // Update is called once per frame
    void Update()
    {
        //相机角度
        {
            //首先根据输入调整球坐标theta和phi
            //如果是平滑操控模式
            if (CameraSmoothFollow)
            {
                float y = Input.GetAxis("Vertical");
                float x = Input.GetAxis("Horizontal");

                //如果有键盘输入，那么就把相机角度插值到鸟的背后
                if (y > 0.9 || y < -0.9 || x > 0.9 || x < -0.9)
                {
                    var thetaphi=LookDirection2ThetaPhi(transform.forward);
                    float targetTheta = thetaphi.x;
                    targetTheta = asureThetaRange(targetTheta);
                    //controlling the camera rotation
                    thetaInDegree = thetaInDegree * (1 - Time.deltaTime * cameraFollowSpeed) + targetTheta * Time.deltaTime * cameraFollowSpeed;


                    float targetPhi = thetaphi.y;
                    targetPhi = asurePhiRange(targetPhi);
                    //controlling the camera rotation
                    phiInDegree = phiInDegree * (1 - Time.deltaTime * cameraFollowSpeed) + targetPhi * Time.deltaTime * cameraFollowSpeed;
                }
                else
                {
                    //controlling the camera rotation
                    phiInDegree -= Input.GetAxis("Mouse X") * tiltAngle * Time.deltaTime * cameraSensitivity;//set up the theta and phi
                    thetaInDegree += Input.GetAxis("Mouse Y") * tiltAngle * Time.deltaTime * cameraSensitivity;
                }
            }
            //非平滑操控模式
            else
            {
                //controlling the camera rotation
                phiInDegree -= Input.GetAxis("Mouse X") * tiltAngle * Time.deltaTime * cameraSensitivity;//set up the theta and phi
                thetaInDegree += Input.GetAxis("Mouse Y") * tiltAngle * Time.deltaTime * cameraSensitivity;
            }

            //把球坐标转换成弧度制
            var phi = phiInDegree / 360 * 2 * Mathf.PI;
            var theta = thetaInDegree / 360 * 2 * Mathf.PI;
            //开始根据theta和phi计算最后的方向
            //如果是平滑模式，则要多做一步插值
            if (CameraSmoothFollow)
            {
                targetLookDir = new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(phi));
                lookDir = Vector3.Slerp(lookDir, targetLookDir, Time.deltaTime * cameraFollowSpeed).normalized;
            }
            //如果不是平滑模式，直接使用就好了
            else
            {
                lookDir = new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(phi));
            }

            virtualCamera.Follow.position = transform.position + lookDir * cameraDist;
        }
        //相机距离和焦距
        {
            if (GetComponent<BirdController>() != null)
            {
                //Debug.Log("cameralensChanging");
                Vector2 fovDist = Vector2.Lerp(normalProjection, highSpeedProjection, GetComponent<BirdController>().cameraLerpFactor);
                Vector2 currentFovDist = new Vector2(virtualCamera.m_Lens.FieldOfView, cameraDist);
                currentFovDist = Vector2.Lerp(currentFovDist, fovDist, projectionChangeSpeed * Time.deltaTime);
                virtualCamera.m_Lens.FieldOfView = currentFovDist.x;
                cameraDist = currentFovDist.y;
            }
        }
    }
    private void OnDisable()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Destroy(virtualCamera.Follow.gameObject);
    }
    private float asurePhiRange(float aPhiAngle)
    {
        aPhiAngle = aPhiAngle > 360 ? aPhiAngle - 360 : aPhiAngle;
        aPhiAngle = aPhiAngle < 0 ? aPhiAngle + 360 : aPhiAngle;
        return aPhiAngle;
    }
    private float asureThetaRange(float aThetaAngle)
    {
        aThetaAngle = aThetaAngle > 160 ? 160 : aThetaAngle;
        aThetaAngle = aThetaAngle < 20 ? 20 : aThetaAngle;
        return aThetaAngle;
    }
    /// <summary>
    /// A function to get theta and phi from a world space direction.
    /// Returns a Vector2,Vector2.x is theta and y is phi
    /// </summary>
    /// <returns></returns>
    private Vector2 LookDirection2ThetaPhi(Vector3 _direction)
    {
        float _Theta = 90 + Mathf.Asin(_direction.y) / (2 * Mathf.PI) * 360f;
        float _Phi = 180f + Mathf.Atan2(_direction.z, _direction.x) / (2 * Mathf.PI) * 360f;
        return new Vector2(_Theta, _Phi);
    }
}
