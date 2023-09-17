using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;


public class PlayerController : MonoBehaviour
{
    public Vector3 pointCamera, frontCamera;
    public float acceleration, maxSpeed, maniability, mass, maxCDBonus;
}

public struct Player : IComponentData
{
    public int Checkpoint, Bonus, restParty;
    public float3 PointCamera, FrontCamera;
    public quaternion Rotation, CurrentRotation;
    public float TimerBegin, Acceleration, MaxSpeed, Maniability, Mass, cdBonus, maxCDBonus,
        VMultip, MMult, AMult, BonMaltimer;
    public bool isFront;
}

public class PlayerBaker : Baker<PlayerController>
{
    public override void Bake(PlayerController authoring)
    {
        AddComponent(new Player
        {
            PointCamera = authoring.pointCamera,
            FrontCamera = authoring.frontCamera,
            Rotation = quaternion.identity,
            CurrentRotation = quaternion.identity,
            Acceleration = authoring.acceleration,
            MaxSpeed = authoring.maxSpeed,
            Maniability = authoring.maniability,
            Mass = authoring.mass,
            cdBonus = authoring.maxCDBonus,
            maxCDBonus = authoring.maxCDBonus,
            BonMaltimer = 0.0f,
            VMultip = 1.0f,
            MMult = 1.0f,
            AMult = 1.0f,
            Bonus = -1,
            TimerBegin = 3,
            Checkpoint = 0,
            restParty = 1,
            isFront = false
        });
    }
}

public partial struct PlayerSystem : ISystem
{
    Unity.Mathematics.Random random;
    NativeArray<float3> positionsMap1, positionsMap2;

    public void OnUpdate(ref SystemState state)
    {
        // si on est pas en partie on quitte directement
        if (Game.mainGame == null) return;
        if (!SystemAPI.HasSingleton<Player>())
            return;
        
        if (!SystemAPI.HasSingleton<Map>())
            return;
            
        Entity entity = SystemAPI.GetSingletonEntity<Player>();
        int IndexMap = SystemAPI.GetSingleton<Map>().IndexMap;

        // selectionne la map en cours
        NativeArray<float3> positions;
        switch (IndexMap)
        {
            case 0:
                positions = positionsMap1;
                break;
            case 1:
                positions = positionsMap2;
                break;
            default:
                return;
        }

        var s = SystemAPI.GetComponentRW<Player>(entity);
        var physic = SystemAPI.GetComponentRW<PhysicsVelocity>(entity);
        var transform = SystemAPI.GetComponentRW<LocalTransform>(entity);

        float dt = Time.deltaTime;
        
        // le 3, 2, 1 au début de la partie
        if (s.ValueRW.TimerBegin > 0)
        {
            s.ValueRW.TimerBegin -= dt;
            Game.mainGame.SetTimerBegin(s.ValueRW.TimerBegin);
            return;
        }
        
        // changer la POV

        if (Game.mainGame.ChangeView())
        {
            s.ValueRW.isFront = !s.ValueRW.isFront;
        }

        // activation des bonus
        
        if (s.ValueRW.Bonus == -1)
        {
            s.ValueRW.cdBonus -= dt;

            if (s.ValueRW.cdBonus < 0.0f)
            {
                s.ValueRW.Bonus = random.NextInt(3);
            }
        }
        else
        {
            if (Game.mainGame.ActiveBonus())
            {
                switch (s.ValueRW.Bonus)
                {
                    case 0:
                        s.ValueRW.VMultip = 3f;
                        s.ValueRW.BonMaltimer = 3;
                        break;
                    case 1:
                        s.ValueRW.MMult = 2f;
                        s.ValueRW.BonMaltimer = 3;
                        break;
                    case 2:
                        s.ValueRW.AMult = 5f;
                        s.ValueRW.BonMaltimer = 3;
                        break;
                }
            }
        }
        
        if (s.ValueRW.BonMaltimer > 0)
        {
            s.ValueRW.BonMaltimer -= dt;
            if (s.ValueRW.BonMaltimer < 0)
            {
                Game.mainGame.ResetBonus();
                s.ValueRW.Bonus = -1;
                s.ValueRW.cdBonus = s.ValueRW.maxCDBonus;
                s.ValueRW.VMultip = 1.0f;
                s.ValueRW.AMult = 1.0f;
                s.ValueRW.MMult = 1.0f;
            }
        }
        
        // pour les mouvement
        physic.ValueRW.Angular = float3.zero;

        float3 directionMove = transform.ValueRW.Forward() * (Game.mainGame.Accelerate()? 1.0f : 0.0f);
        
        var move = Game.mainGame.Move();

        s.ValueRW.Rotation *= 
            Quaternion.Euler(
                -move.y * s.ValueRW.Maniability * s.ValueRW.MMult * dt,
                move.x * s.ValueRW.Maniability * s.ValueRW.MMult * dt,
                Game.mainGame.RotateShip() * s.ValueRW.Maniability * s.ValueRW.MMult * dt);
        
        s.ValueRW.CurrentRotation = 
            Quaternion.Lerp(s.ValueRW.CurrentRotation, 
                s.ValueRW.Rotation, 5.0f * dt);
        
        transform.ValueRW.Rotation = s.ValueRW.CurrentRotation;

        // vitesse actuelle / vitesse max pour pas franchir la limite
        physic.ValueRW.Linear += s.ValueRW.Acceleration * s.ValueRW.AMult * s.ValueRW.VMultip *
                                 (1.0f - math.length(physic.ValueRW.Linear) / s.ValueRW.MaxSpeed) * 
                                 dt * s.ValueRW.VMultip * directionMove;

        physic.ValueRW.Linear *= 1.0f - s.ValueRW.Mass * dt;

        int cpt = 0;

        float dirReste = math.lengthsq(positions[s.ValueRW.Checkpoint % positions.Length] - transform.ValueRW.Position);

        // pour savoir la position du joueur dans la partie
        foreach (var e in SystemAPI.Query<RefRW<SpaceShip>>())
        {
            var ss = e.ValueRW;

            if (ss.Tour < s.ValueRW.restParty ||
                (ss.Tour == s.ValueRW.restParty && ss.Checkpoint > s.ValueRW.Checkpoint) ||
                (ss.Tour == s.ValueRW.restParty && ss.Checkpoint == s.ValueRW.Checkpoint && ss.DistCheck < dirReste))
            {
                cpt++;
            }
        }
        
        // affichage de la position
        Game.mainGame.GetPositionInGame(cpt + 1);
        
        // pour finir la partie plus vite
        if (Input.GetKeyDown(KeyCode.F8))
        {
            Game.mainGame.EndGame(cpt + 1);
            return;
        }

        // update le chackpoint et affiche la fin de la partie si nécéssaire
        if (dirReste < 10000)
        {
            s.ValueRW.Checkpoint++;
            if (s.ValueRW.Checkpoint % positions.Length == 0 && --s.ValueRW.restParty == 0)
            {
                Game.mainGame.EndGame(cpt + 1);
                return;
            }
        }
        
        // set les valeur à l'écran

        Game.mainGame.SetBonMaltimer(s.ValueRW.BonMaltimer);
        Game.mainGame.SetBonus(s.ValueRW.Bonus, s.ValueRW.cdBonus / s.ValueRW.maxCDBonus);
        Game.mainGame.SetSpeed(math.length(physic.ValueRW.Linear));
        Game.mainGame.setDestination(positions[s.ValueRW.Checkpoint % positions.Length],
            s.ValueRW.Checkpoint == positions.Length - 1);
        Game.mainGame.setNextDestination(positions[(s.ValueRW.Checkpoint + 1) % positions.Length]);
        Game.mainGame.CameraPosition(
            transform.ValueRW.TransformPoint(
                s.ValueRW.isFront? s.ValueRW.FrontCamera : s.ValueRW.PointCamera),
            transform.ValueRW.Rotation, s.ValueRW.isFront? 100.0f : 5.0f);
    }
    
    public void OnCreate(ref SystemState state)
    {
        random = new ((uint) UnityEngine.Random.Range(0, uint.MaxValue));

        // les 2 maps
        positionsMap1 = new(new []
         {
             new float3(0f, 265.62f, 264.9f),
             new float3(52.90362f, 322.7832f, 444.1713f),
             new float3(111.7475f, 381.0842f, 592.2925f),
             new float3(176.5953f, 438.166f, 714.0859f),
             new float3(247.2147f, 492.4536f, 813.8907f),
             new float3(323.1374f, 542.9792f, 895.5754f),
             new float3(403.7105f, 589.2334f, 962.5508f),
             new float3(488.1439f, 631.049f, 1017.798f),
             new float3(575.5488f, 668.5056f, 1063.893f),
             new float3(664.9731f, 701.8524f, 1103.039f),
             new float3(755.4315f, 731.4528f, 1137.101f),
             new float3(845.9291f, 757.7332f, 1167.626f),
             new float3(935.4842f, 781.1524f, 1195.884f),
             new float3(1023.145f, 802.1744f, 1222.89f),
             new float3(1108.003f, 821.2513f, 1249.432f),
             new float3(1189.209f, 838.8108f, 1276.096f),
             new float3(1265.975f, 855.2479f, 1303.288f),
             new float3(1337.586f, 870.9216f, 1331.254f),
             new float3(1403.405f, 886.155f, 1360.103f),
             new float3(1462.873f, 901.2307f, 1389.817f),
             new float3(1515.511f, 916.3959f, 1420.271f),
             new float3(1560.924f, 931.8619f, 1451.248f),
             new float3(1598.795f, 947.8077f, 1482.447f),
             new float3(1628.892f, 964.3828f, 1513.505f),
             new float3(1651.056f, 981.7048f, 1543.997f),
             new float3(1665.206f, 999.8656f, 1573.453f),
             new float3(1671.33f, 1018.929f, 1601.367f),
             new float3(1669.49f, 1038.935f, 1627.206f),
             new float3(1659.812f, 1059.896f, 1650.421f),
             new float3(1642.483f, 1081.8f, 1670.452f),
             new float3(1617.752f, 1104.612f, 1686.74f),
             new float3(1585.923f, 1128.269f, 1698.734f),
             new float3(1547.353f, 1152.685f, 1705.896f),
             new float3(1502.444f, 1177.748f, 1707.71f),
             new float3(1451.649f, 1203.323f, 1703.692f),
             new float3(1395.459f, 1229.25f, 1693.387f),
             new float3(1334.405f, 1255.348f, 1676.386f),
             new float3(1269.05f, 1281.412f, 1652.319f),
             new float3(1199.993f, 1307.22f, 1620.869f),
             new float3(1127.858f, 1332.532f, 1581.773f),
             new float3(1053.294f, 1357.094f, 1534.827f),
             new float3(976.9702f, 1380.639f, 1479.881f),
             new float3(899.5717f, 1402.893f, 1416.854f),
             new float3(821.796f, 1423.577f, 1345.724f),
             new float3(744.3492f, 1442.412f, 1266.541f),
             new float3(667.9388f, 1459.121f, 1179.416f),
             new float3(593.2696f, 1473.435f, 1084.528f),
             new float3(521.041f, 1485.097f, 982.1241f),
             new float3(451.9374f, 1493.863f, 872.5157f),
             new float3(386.6246f, 1499.512f, 756.08f),
             new float3(325.7431f, 1501.843f, 633.2562f),
             new float3(269.9024f, 1500.684f, 504.5464f),
             new float3(219.6743f, 1495.891f, 370.5108f),
             new float3(175.5863f, 1487.356f, 231.7656f),
             new float3(138.1151f, 1475.007f, 88.97865f),
             new float3(107.6815f, 1458.809f, -57.13045f),
             new float3(84.64262f, 1438.769f, -205.8009f),
             new float3(69.28723f, 1414.935f, -356.2311f),
             new float3(61.83041f, 1387.399f, -507.5857f),
             new float3(62.40874f, 1356.298f, -659.0012f),
             new float3(71.07616f, 1321.812f, -809.5936f),
             new float3(87.80076f, 1284.165f, -958.4617f),
             new float3(112.4622f, 1243.623f, -1104.7f),
             new float3(144.8503f, 1200.494f, -1247.405f),
             new float3(184.6644f, 1155.124f, -1385.681f),
             new float3(231.5135f, 1107.896f, -1518.652f),
             new float3(284.9185f, 1059.223f, -1645.472f),
             new float3(344.3129f, 1009.547f, -1765.331f),
             new float3(409.0483f, 959.3307f, -1877.465f),
             new float3(478.3978f, 909.0518f, -1981.17f),
             new float3(551.561f, 859.1979f, -2075.805f),
             new float3(627.6705f, 810.2568f, -2160.804f),
             new float3(705.7986f, 762.7084f, -2235.684f),
             new float3(784.9633f, 717.0163f, -2300.046f),
             new float3(864.1382f, 673.6171f, -2353.588f),
             new float3(942.259f, 632.9108f, -2396.103f),
             new float3(1018.233f, 595.2509f, -2427.482f),
             new float3(1090.945f, 560.9341f, -2447.712f),
             new float3(1159.271f, 530.1917f, -2456.876f),
             new float3(1222.082f, 503.1784f, -2455.141f),
             new float3(1278.255f, 479.9678f, -2442.751f),
             new float3(1326.685f, 460.5432f, -2420.014f),
             new float3(1366.29f, 444.7943f, -2387.286f),
             new float3(1396.027f, 432.5155f, -2344.949f),
             new float3(1414.902f, 423.4055f, -2293.388f),
             new float3(1421.983f, 417.0709f, -2232.961f),
             new float3(1416.419f, 413.0333f, -2163.973f),
             new float3(1397.458f, 410.7384f, -2086.636f),
             new float3(1364.469f, 409.5704f, -2001.035f),
             new float3(1316.972f, 408.8679f, -1907.087f),
             new float3(1254.673f, 407.9438f, -1804.5f),
             new float3(1177.502f, 406.1068f, -1692.728f),
             new float3(1085.67f, 402.6844f, -1570.927f),
             new float3(979.7206f, 397.0462f, -1437.908f),
             new float3(860.608f, 388.6239f, -1292.097f),
             new float3(729.7826f, 376.9285f, -1131.481f),
             new float3(589.2927f, 361.5596f, -953.5731f),
             new float3(441.9051f, 342.2031f, -755.3589f),
             new float3(291.251f, 318.6161f, -533.2619f),
             new float3(141.9884f, 290.5883f, -283.091f),
             new float3(0f, 257.88f, 0f),
         }, Allocator.Persistent);
         
         positionsMap2 = new (new [] {
            new float3(0f, 0f, 10f),
            new float3(-2.229749f, 7.159284f, 161.85f),
            new float3(-8.623011f, 25.75961f, 287.6617f),
            new float3(-18.48437f, 52.2161f, 391.0178f),
            new float3(-30.9129f, 83.7599f, 474.7247f),
            new float3(-44.92834f, 118.271f, 541.0585f),
            new float3(-59.56245f, 154.1426f, 591.939f),
            new float3(-73.92439f, 190.1733f, 629.0544f),
            new float3(-87.24519f, 225.4808f, 653.9451f),
            new float3(-98.90613f, 259.4333f, 668.0541f),
            new float3(-108.4542f, 291.5969f, 672.7609f),
            new float3(-115.6064f, 321.6918f, 669.3903f),
            new float3(-120.2456f, 349.5602f, 659.218f),
            new float3(-122.4101f, 375.1405f, 643.4654f),
            new float3(-122.2765f, 398.4458f, 623.2914f),
            new float3(-120.1392f, 419.5485f, 599.7814f),
            new float3(-116.3865f, 438.5665f, 573.9355f),
            new float3(-111.4753f, 455.6526f, 546.6605f),
            new float3(-105.9053f, 470.9866f, 518.762f),
            new float3(-100.1921f, 484.7648f, 490.9355f),
            new float3(-94.84315f, 497.196f, 463.7659f),
            new float3(-90.33507f, 508.4937f, 437.7252f),
            new float3(-87.09327f, 518.8718f, 413.1739f),
            new float3(-85.47554f, 528.5403f, 390.3662f),
            new float3(-85.75822f, 537.6989f, 369.4528f),
            new float3(-88.12708f, 546.535f, 350.4904f),
            new float3(-92.6716f, 555.2198f, 333.4493f),
            new float3(-99.38357f, 563.9061f, 318.2237f),
            new float3(-108.1586f, 572.7236f, 304.6409f),
            new float3(-118.8024f, 581.7803f, 292.4737f),
            new float3(-131.0388f, 591.1586f, 281.4497f),
            new float3(-144.5213f, 600.916f, 271.2635f),
            new float3(-158.8463f, 611.0831f, 261.5857f),
            new float3(-173.5684f, 621.6647f, 252.0734f),
            new float3(-188.2159f, 632.6407f, 242.3801f),
            new float3(-202.3076f, 643.9656f, 232.1624f),
            new float3(-215.3691f, 655.571f, 221.0889f),
            new float3(-226.9479f, 667.3657f, 208.8456f),
            new float3(-236.6289f, 679.2396f, 195.1422f),
            new float3(-244.0464f, 691.0649f, 179.7159f),
            new float3(-248.8962f, 702.6986f, 162.3353f),
            new float3(-250.9427f, 713.9836f, 142.8024f),
            new float3(-250.0273f, 724.754f, 120.9548f),
            new float3(-246.0711f, 734.8358f, 96.66569f),
            new float3(-239.0769f, 744.051f, 69.84542f),
            new float3(-229.1272f, 752.2192f, 40.43932f),
            new float3(-216.3813f, 759.1607f, 8.427413f),
            new float3(-201.0694f, 764.7004f, -26.17647f),
            new float3(-183.4847f, 768.6682f, -63.32761f),
            new float3(-163.9737f, 770.9039f, -102.9517f),
            new float3(-142.9258f, 771.2565f, -144.9466f),
            new float3(-120.7613f, 769.5895f, -189.1854f),
            new float3(-97.919f, 765.7813f, -235.5176f),
            new float3(-74.84411f, 759.7277f, -283.7719f),
            new float3(-51.97521f, 751.3424f, -333.7574f),
            new float3(-29.7333f, 740.5598f, -385.265f),
            new float3(-8.510191f, 727.3353f, -438.07f),
            new float3(11.34094f, 711.647f, -491.9323f),
            new float3(29.51341f, 693.4957f, -546.5978f),
            new float3(45.7539f, 672.9065f, -601.7995f),
            new float3(59.86705f, 649.9283f, -657.2578f),
            new float3(71.71799f, 624.6346f, -712.6811f),
            new float3(81.23372f, 597.1234f, -767.7672f),
            new float3(88.40166f, 567.5169f, -822.2036f),
            new float3(93.26691f, 535.9614f, -875.6681f),
            new float3(95.92802f, 502.6262f, -927.8302f),
            new float3(96.53081f, 467.7036f, -978.3527f),
            new float3(95.26144f, 431.4081f, -1026.892f),
            new float3(92.33855f, 393.9747f, -1073.103f),
            new float3(88.00455f, 355.6578f, -1116.638f),
            new float3(82.51676f, 316.73f, -1157.153f),
            new float3(76.1387f, 277.4806f, -1194.31f),
            new float3(69.13156f, 238.213f, -1227.781f),
            new float3(61.74639f, 199.2441f, -1257.253f),
            new float3(54.21702f, 160.9f, -1282.431f),
            new float3(46.75431f, 123.5142f, -1303.048f),
            new float3(39.54147f, 87.4247f, -1318.865f),
            new float3(32.73081f, 52.96977f, -1329.681f),
            new float3(26.44194f, 20.4845f, -1335.335f),
            new float3(20.7613f, -9.704217f, -1335.712f),
            new float3(15.74318f, -37.28202f, -1330.749f),
            new float3(11.4117f, -61.95397f, -1320.436f),
            new float3(7.76403f, -83.4502f, -1304.821f),
            new float3(4.774401f, -101.5333f, -1284.007f),
            new float3(2.39859f, -116.0064f, -1258.156f),
            new float3(0.5787411f, -126.7214f, -1227.48f),
            new float3(-0.751748f, -133.5886f, -1192.243f),
            new float3(-1.663707f, -136.5869f, -1152.748f),
            new float3(-2.22805f, -135.7739f, -1109.329f),
            new float3(-2.512383f, -131.2973f, -1062.337f),
            new float3(-2.578447f, -123.4056f, -1012.126f),
            new float3(-2.480501f, -112.459f, -959.0346f),
            new float3(-2.264673f, -98.93928f, -903.3635f),
            new float3(-1.969232f, -83.45895f, -845.3542f),
            new float3(-1.625703f, -66.76726f, -785.1633f),
            new float3(-1.260649f, -49.75369f, -722.835f),
            new float3(-0.8979475f, -33.44646f, -658.2722f),
            new float3(-0.5613618f, -19.00467f, -591.2054f),
            new float3(-0.2772212f, -7.702191f, -521.1614f),
            new float3(-0.07703564f, -0.8992372f, -447.4248f),
            new float3(0f, 0f, -369f),
            new float3(0f, 0f, 10f),
            }, Allocator.Persistent);
    }

    public void OnDestroy(ref SystemState state)
    {
        positionsMap1.Dispose();
        positionsMap2.Dispose();
    }
}