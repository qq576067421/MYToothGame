// using Unity.VisualScripting;
// using UnityEngine;

// public class GyroInfoDemo : GyroInfo
// {
//     [SerializeField] private Transform targetCube; // 要控制的Cube

//     [Header("控制参数")]
//     [SerializeField] private bool usePhysicsForces = true;
//     [SerializeField] DebugUI debugUI;

//     private Rigidbody cubeRigidbody;

//     protected override void Awake()
//     {
//         // 先调用父类的Awake
//         base.Awake();

//         InitializeDemo();
//     }
//     private void InitializeDemo()
//     {
//         // 确保有目标Cube
//         if (targetCube == null)
//         {
//             // 尝试查找场景中的Cube
//             GameObject cube = GameObject.Find("Cube");
//             if (cube != null)
//             {
//                 targetCube = cube.transform;
//             }
//             else
//             {
//                 // 创建一个新的Cube
//                 GameObject newCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
//                 newCube.name = "GyroDemoCube";
//                 targetCube = newCube.transform;
//                 targetCube.position = new Vector3(0, 2, 0);
//             }
//         }

//         // 添加或获取Rigidbody组件
//         cubeRigidbody = targetCube.GetComponent<Rigidbody>();
//         if (cubeRigidbody == null)
//         {
//             cubeRigidbody = targetCube.gameObject.AddComponent<Rigidbody>();
//         }

//         // 根据模式设置Rigidbody属性
//         if (usePhysicsForces)
//         {
//             // 物理模式：启用物理特性
//             cubeRigidbody.useGravity = false;
//             cubeRigidbody.isKinematic = false;
//             cubeRigidbody.linearDamping = 0.1f;
//             cubeRigidbody.angularDamping = 0.5f;
//         }
//         else
//         {
//             // 非物理模式：禁用物理特性
//             cubeRigidbody.useGravity = false;
//             cubeRigidbody.isKinematic = true; // 关键：设置为运动学，不受物理影响
//         }

//         ConfigurePhysicsMaterial();
//         Debug.Log("[GyroInfoDemo] Demo模式初始化完成");
//     }

//     /// <summary>
//     /// 配置物理材质（使用新方法）
//     /// </summary>
//     private void ConfigurePhysicsMaterial()
//     {
//         Collider collider = targetCube.GetComponent<Collider>();
//         if (collider != null)
//         {
//             // 创建新的PhysicsMaterial实例
//             PhysicsMaterial physicsMaterial = new PhysicsMaterial()
//             {
//                 dynamicFriction = 0.1f,
//                 staticFriction = 0.1f,
//                 bounciness = 0.8f,
//                 frictionCombine = PhysicsMaterialCombine.Multiply,
//                 bounceCombine = PhysicsMaterialCombine.Average
//             };

//             collider.material = physicsMaterial;
//         }
//     }

//     public new void Update()
//     {
//         Debug.Log("GyroInfoDemo= 每一帧都调用");
//         base.Update();
//         // 应用陀螺仪数据到Cube（使用GyroInfoDataDemo的数据）
//         ApplyGyroDataToCube();

//         if (Input.gyro.enabled)
//         {
//             Debug.Log("Unity Input陀螺仪有效！");
//             Debug.Log($"加速度：{Input.gyro.userAcceleration}, 角速度：{Input.gyro.rotationRate}");
//         }
//         else
//         {
//             Debug.Log("Unity Input陀螺仪无效！");
//         }
//     }

//     /// <summary>
//     /// 将陀螺仪数据应用到Cube
//     /// </summary>
//     private void ApplyGyroDataToCube()
//     {
//         if (targetCube == null || cubeRigidbody == null) return;

//         // 从父类获取设备0的所有陀螺仪数据数组
//         YouDooSDKConstants.GyroData[] gyroDataArray = GetDeviceGyroData(0);

//         if (gyroDataArray == null || gyroDataArray.Length == 0)
//         {
//             Debug.LogWarning("[GyroInfoDemo] 设备0无有效陀螺仪数据");
//             return;
//         }

//         // 打印设备0的所有数据点
//         Debug.Log($"[GyroInfoDemo] === 设备0获取到 {gyroDataArray.Length} 个数据点 ===");
//         for (int i = 0; i < gyroDataArray.Length; i++)
//         {
//             var data = gyroDataArray[i];
//             Debug.Log($"[GyroInfoDemo] 新业务代码使用 设备0[数据点{i}]: 时间戳={data.timestamp}, " +
//                      $"加速度=({data.accelX},{data.accelY},{data.accelZ}), " +
//                      $"陀螺仪=({data.gyroX},{data.gyroY},{data.gyroZ})");
//         }

//         // 使用最新的数据点（第一个）
//         var latestGyroData = gyroDataArray[0];

//         // 更严格的数据有效性检查
//         if (latestGyroData.timestamp == 0)
//         {
//             Debug.LogWarning("[GyroInfoDemo] 设备0最新数据无效");
//             return;
//         }

//         // 将整数数据转换为Unity的Vector3
//         // 轴对齐，并添加灵敏度修正
//         Vector3 acceleration = new Vector3(
//             latestGyroData.accelX,
//             latestGyroData.accelZ,
//             -latestGyroData.accelY
//             );
//         Vector3 gyro = new Vector3(
//             latestGyroData.gyroX,
//             latestGyroData.gyroZ,
//             -latestGyroData.gyroY
//             ) * debugUI.GyroSen;

        

//         Debug.Log($"[GyroInfoDemo] 使用设备0最新数据 - 时间戳: {latestGyroData.timestamp}, " +
//                  $"加速度: [{acceleration.x}, {acceleration.y}, {acceleration.z}], " +
//                  $"陀螺仪: [{gyro.x}, {gyro.y}, {gyro.z}]");


//         // 将数据传给DebugUI显示
//         debugUI.UpdateUIData(acceleration, gyro);

//         if (usePhysicsForces)
//         {
//             ApplyPhysicsForces(acceleration, gyro);
//         }
//         else
//         {
//             ApplyDirectTransform(acceleration, gyro);
//         }
//     }

//     /// <summary>
//     /// 使用物理力控制Cube
//     /// </summary>
//     private void ApplyPhysicsForces(Vector3 acceleration, Vector3 gyro)
//     {
//         // force：要施加的力的大小和方向（Vector3）
//         Vector3 force = acceleration;

//         //Time.deltaTime：乘以这个是为了让力的作用与帧率无关，确保在不同帧率下运动一致
//         //ForceMode.Force：力的模式，表示持续施加力
//         cubeRigidbody.AddForce(force * Time.deltaTime, ForceMode.Force);   //给 Cube 施加一个线性力，让它移动

//         // 大幅增加扭矩的倍数
//         Vector3 torque = gyro;

//         cubeRigidbody.AddTorque(torque * Time.deltaTime, ForceMode.Force); //给 Cube 施加一个旋转力（扭矩），让它旋转

//         Debug.Log($"[GyroInfoDemo] 施加力: {force}, 扭矩: {torque}");
//     }

//     /// <summary>
//     /// 直接变换控制（非物理）
//     /// </summary>
//     private void ApplyDirectTransform(Vector3 acceleration, Vector3 gyro)
//     {
//         // 陀螺仪设备的坐标系是左手坐标系，左手旋转定则
//         // 正手持遥控器时（灯在前，按键朝上，竖持）
//         // 右X+，后Y+，上Z+

//         // 滤波测试
//         // debugUI.ApplyFilter(acceleration, gyro, Time.deltaTime);
//         Debug.Log($"[GyroInfoDemo] Gyro: {gyro}, Acceleration: {acceleration}");
//     }

//     /// <summary>
//     /// 重置Cube状态
//     /// </summary>
//     public void ResetCube()
//     {
//         if (targetCube != null)
//         {
//             targetCube.position = new Vector3(0, 2, 0);
//             targetCube.rotation = Quaternion.identity;

//             if (cubeRigidbody != null)
//             {
//                 cubeRigidbody.linearVelocity = Vector3.zero;
//                 cubeRigidbody.angularVelocity = Vector3.zero;
//             }
//         }
//     }

//     private void OnDestroy()
//     {
//         // 清理资源
//         if (cubeRigidbody != null)
//         {
//             Collider collider = targetCube.GetComponent<Collider>();
//             if (collider != null && collider.material != null)
//             {
//                 Destroy(collider.material);
//             }
//         }
//     }
// }