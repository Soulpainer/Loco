// using UnityEngine;
// using UnityEngine.UI;
// using System.Collections.Generic;

// public class PlatformConnectionManager : MonoBehaviour
// {
//     [Header("UI")]
//     [SerializeField] private Camera worldCamera;
//     [SerializeField] private Canvas worldCanvas;
//     [SerializeField] private Button connectionButtonPrefab;

//     [Header("Settings")]
//     [SerializeField] private float disconnectImpulse = 5f;

//     private class ConnectionUI
//     {
//         public SplineMotor a;
//         public SplineMotor b;
//         public PlatformWithMotors platformA;
//         public PlatformWithMotors platformB;
//         public Button button;
//     }

//     private readonly List<ConnectionUI> _connections = new();

//     private void Awake()
//     {
//         if (!worldCamera)
//             worldCamera = Camera.main;
//     }

//     private void Update()
//     {
//         UpdateButtonPositions();
//     }

//     // Вызывается когда две платформы соединяются
//     public bool TryConnectMotors(SplineMotor a, SplineMotor b)
//     {
//         if (a == null || b == null)
//             return false;

//         if (a.TryToConnect(b))
//         {
//             var button = Instantiate(connectionButtonPrefab, worldCanvas.transform);
//             var platformA = a.GetComponentInParent<PlatformWithMotors>();
//             var platformB = b.GetComponentInParent<PlatformWithMotors>();

//             var ui = new ConnectionUI
//             {
//                 a = a,
//                 b = b,
//                 platformA = platformA,
//                 platformB = platformB,
//                 button = button
//             };

//             button.onClick.AddListener(() => Disconnect(ui));
//             _connections.Add(ui);
//             return true;
//         }

//         return false;
//     }

//     private void UpdateButtonPositions()
//     {
//         foreach (var c in _connections)
//         {
//             if (c.a == null || c.b == null)
//                 continue;

//             Vector3 worldMid = (c.a.transform.position + c.b.transform.position) * 0.5f;
//             Vector3 screenPos = worldCamera.WorldToScreenPoint(worldMid);

//             bool visible = screenPos.z > 0f;
//             c.button.gameObject.SetActive(visible);

//             if (visible)
//                 c.button.transform.position = screenPos;
//         }
//     }

//     private void Disconnect(ConnectionUI ui)
//     {
//         if (ui.a == null || ui.b == null)
//         {
//             Destroy(ui.button.gameObject);
//             _connections.Remove(ui);
//             return;
//         }

//         // Разрыв связи
//         ui.a.ConnectedTo = null;
//         ui.b.ConnectedTo = null;

//         // Добавляем "отталкивание" платформ
//         if (ui.platformA && ui.platformB)
//         {
//             Vector3 dir = (ui.a.transform.position - ui.b.transform.position).normalized;
//             float dot = Vector3.Dot(dir, (ui.platformA.frontMotor.transform.position - ui.platformA.backMotor.transform.position).normalized);

//             // Простое направление: если мотор смотрит "вдоль" направления — прибавляем, иначе вычитаем
//             float impulseA = disconnectImpulse * (dot >= 0 ? 1f : -1f);
//             float impulseB = -impulseA;

//             ui.platformA.AddSpeed(impulseA);
//             ui.platformB.AddSpeed(impulseB);
//         }

//         Destroy(ui.button.gameObject);
//         _connections.Remove(ui);
//     }
// }
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class PlatformConnectionManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private Button connectionButtonPrefab;

    [Header("Settings")]
    [SerializeField] private float disconnectImpulse = 5f;
    [SerializeField] private float connectDistanceThreshold = 2.5f; // при каком расстоянии показывать кнопку "Сцепить"
    [SerializeField] private float hideDistanceThreshold = 4.0f;    // при каком расстоянии скрывать кнопку

    private class ConnectionUI
    {
        public SplineMotor a;
        public SplineMotor b;
        public PlatformWithMotors platformA;
        public PlatformWithMotors platformB;
        public Button button;
        public bool connected; // true = сцеплены, false = готовы к сцепке
    }

    private readonly List<ConnectionUI> _connections = new();

    private void Awake()
    {
        if (!worldCamera)
            worldCamera = Camera.main;
    }

    private void Update()
    {
        UpdateButtonPositionsAndStates();
    }

    // 📌 Автоматическая сцепка (вызов из PlatformWithMotors.ResolvePenetration)
    public bool TryAutoConnect(SplineMotor a, SplineMotor b)
    {
        if (a == null || b == null) return false;

        // Если уже соединены — ничего не делаем
        if (a.ConnectedTo == b || b.ConnectedTo == a)
            return true;

        if (a.ConnectedTo != null || b.ConnectedTo != null)
            return false;

        var platformA = a.GetComponentInParent<PlatformWithMotors>();
        var platformB = b.GetComponentInParent<PlatformWithMotors>();

        if (!platformA.CanBeConnected || !platformB.CanBeConnected)
            return false;

        // Можно ли соединить автоматически
        bool canAuto = a.AutoConnect && b.AutoConnect && a.ConnectedTo == null && b.ConnectedTo == null;// a.TryToConnect(b);
        if (canAuto)
        {
            ConnectionUI ui = CreateConnectionUI(a, b, connected: true);
            if (ui != null) ConnectManually(ui);
            return true;
        }

        // Если не получилось — создаем кнопку "Сцепить"
        CreateConnectionUI(a, b, connected: false);
        return false;
    }

    private ConnectionUI CreateConnectionUI(SplineMotor a, SplineMotor b, bool connected)
    {
        foreach (var c in _connections)
        {
            if ((c.a == a && c.b == b) || (c.a == b && c.b == a))
                return null;
        }

        var button = Instantiate(connectionButtonPrefab, worldCanvas.transform);
        button.GetComponentInChildren<TMP_Text>().text = connected ? "><" : "<>";
        var platformA = a.GetComponentInParent<PlatformWithMotors>();
        var platformB = b.GetComponentInParent<PlatformWithMotors>();

        var ui = new ConnectionUI
        {
            a = a,
            b = b,
            platformA = platformA,
            platformB = platformB,
            button = button,
            connected = connected
        };

        UpdateButtonText(ui);

        if (connected)
            button.onClick.AddListener(() => Disconnect(ui));
        else
            button.onClick.AddListener(() => ConnectManually(ui));

        _connections.Add(ui);

        return ui;
    }

    private void UpdateButtonText(ConnectionUI ui)
    {
        var text = ui.button.GetComponentInChildren<Text>();
        if (text)
            text.text = ui.connected ? "Разъединить" : "Сцепить";
    }

    private void UpdateButtonPositionsAndStates()
    {
        List<ConnectionUI> toRemove = new();

        foreach (var c in _connections)
        {
            if (c.a == null || c.b == null)
            {
                toRemove.Add(c);
                continue;
            }

            Vector3 worldMid = (c.a.transform.position + c.b.transform.position) * 0.5f;
            Vector3 screenPos = worldCamera.WorldToScreenPoint(worldMid);
            float distance = Vector3.Distance(c.a.transform.position, c.b.transform.position);

            bool visible = screenPos.z > 0f;

            if (!visible || distance > hideDistanceThreshold)
            {
                c.button.gameObject.SetActive(false);
                if (!c.connected) // для незакреплённых кнопка исчезает навсегда
                    toRemove.Add(c);
                continue;
            }

            // показываем кнопку только если в зоне видимости
            c.button.gameObject.SetActive(true);
            c.button.transform.position = screenPos;

            // Если вагоны сблизились — позволяем сцепить вручную
            if (!c.connected && distance < connectDistanceThreshold)
            {
                c.button.interactable = true;
            }
            else if (!c.connected)
            {
                c.button.interactable = false;
            }
        }

        // Удаляем "мертвые" кнопки
        foreach (var c in toRemove)
        {
            if (c.button)
                Destroy(c.button.gameObject);
            _connections.Remove(c);
        }
    }

    // 🔗 Ручное соединение
    private void ConnectManually(ConnectionUI ui)
    {
        if (ui.a.TryToConnect(ui.b))
        {
            ui.connected = true;
            UpdateButtonText(ui);

            ui.button.onClick.RemoveAllListeners();
            ui.button.onClick.AddListener(() => Disconnect(ui));
            ui.button.GetComponentInChildren<TMP_Text>().text = "><";
        }
    }

    //     // ❌ Разъединение
    //     private void Disconnect(ConnectionUI ui)
    //     {
    //         if (ui.a == null || ui.b == null)
    //         {
    //             if (ui.button)
    //                 Destroy(ui.button.gameObject);
    //             _connections.Remove(ui);
    //             return;
    //         }

    //         ui.a.ConnectedTo = null;
    //         ui.b.ConnectedTo = null;

    //         // "Отталкивание" — через AddSpeed
    //         if (ui.platformA && ui.platformB)
    //         {
    //             // Vector3 dir = (ui.a.transform.position - ui.b.transform.position).normalized;
    //             // ui.platformA.AddSpeed(dir*disconnectImpulse);
    //             // ui.platformB.AddSpeed(-dir*disconnectImpulse);
    //             float dirA = (ui.platformA.frontMotor.S + ui.platformA.backMotor.S) / 2f <
    //              (ui.platformB.frontMotor.S + ui.platformB.backMotor.S) / 2f ? -1f : 1f;
    //             float dirB = -dirA;

    //             ui.platformA.AddSpeed(disconnectImpulse * dirA);
    //             ui.platformB.AddSpeed(disconnectImpulse * dirB);
    //         }

    //         if (ui.button)
    //             Destroy(ui.button.gameObject);

    //         _connections.Remove(ui);
    //     }
    private void Disconnect(ConnectionUI ui)
    {
        if (ui == null || ui.a == null || ui.b == null)
            return;

        // Логический разрыв
        ui.a.ConnectedTo = null;
        ui.b.ConnectedTo = null;
        ui.connected = false;

        // "Отталкивание" платформ
        if (ui.platformA != null && ui.platformB != null)
        {
            if (ui.platformA.frontMotor == ui.a)
            {
                ui.platformA.SetTmpSpeed(disconnectImpulse);
                ui.platformB.SetTmpSpeed(-disconnectImpulse);
            }
            else
            {
                ui.platformA.SetTmpSpeed(-disconnectImpulse);
                ui.platformB.SetTmpSpeed(disconnectImpulse);
            }
        }

        // Убираем кнопку
        if (ui.button)
        {
            ui.button.onClick.RemoveAllListeners();
            Destroy(ui.button.gameObject);
        }

        _connections.Remove(ui);
    }

}
