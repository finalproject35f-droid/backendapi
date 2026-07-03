# Flutter Integration Contract

Give the Flutter developer the deployed API base URL:

```text
https://epicare-api-production.up.railway.app
```

In Flutter, configure it with:

```powershell
flutter run --dart-define=EPICARE_API_BASE_URL=https://epicare-api-production.up.railway.app
```

## States

```text
0 = Normal
1 = Warning
2 = Seizure
3 = Offline
```

Embedded command codes:

```text
N = Normal
P = Warning / high seizure risk
S = Seizure detected
```

## Main Endpoints

### Health

```http
GET /api/health
```

### Latest Patient State

Use this for the patient home status card.

Recommended compact endpoint:

```http
GET /api/seizure/latest
```

Example response:

```json
{
  "prediction": {
    "state": "SEIZURE_DETECTED",
    "code": "S",
    "probability": 0.85
  },
  "sensors": {
    "eeg": [512, 510],
    "ecg": 345,
    "emg": 278,
    "acc": [2.1, -1.4, 980]
  },
  "timestamp": "2026-05-19T14:30:00Z",
  "deviceId": "proteus-01"
}
```

Full monitoring endpoint:

```http
GET /api/patients/demo-patient/latest
```

Example response:

```json
{
  "patientId": "demo-patient",
  "deviceId": "manual-test",
  "state": 2,
  "command": "S",
  "latestReading": {
    "eeg": [610, 548],
    "ecg": 64,
    "emg": 220,
    "acc": [2.1, -1.4, 980],
    "state": "S"
  },
  "prediction": {
    "label": "Seizure",
    "probability": 0.95,
    "source": "fallback-ai-http-error"
  },
  "isConnected": true
}
```

### Recent Readings

Use this for charts.

```http
GET /api/patients/demo-patient/readings/latest?take=50
```

### Alerts

Use this for recent alerts list.

```http
GET /api/patients/demo-patient/alerts
```

### Seizure Events

Use this for seizure history.

```http
GET /api/patients/demo-patient/seizure-events
```

### Manual AI Prediction

Use this only for testing the model endpoint through the backend.

```http
POST /api/predictions/run
Content-Type: application/json
```

Body:

```json
{
  "eeg": [
    [
      [0.1, 0.2, "... 3840 samples"],
      [0.1, 0.2, "... 3840 samples"]
    ],
    "... 8 windows"
  ],
  "ecg": [
    [
      [0.1, 0.2, "... 3840 samples"]
    ],
    "... 8 windows"
  ],
  "emg": [
    [
      [0.1, 0.2, "... 3840 samples"]
    ],
    "... 8 windows"
  ]
}
```

Model input shape:

```text
EEG: (8, 2, 3840)
ECG: (8, 1, 3840)
EMG: (8, 1, 3840)
```

## Demo Input Endpoint

The simulator/bridge posts to:

```http
POST /api/iot/readings
```

Normal:

```json
{
  "patientId": "demo-patient",
  "deviceId": "manual-test",
  "eeg": [502, 498],
  "ecg": 72,
  "emg": 18,
  "state": "N"
}
```

Warning:

```json
{
  "patientId": "demo-patient",
  "deviceId": "manual-test",
  "eeg": [540, 531],
  "ecg": 92,
  "emg": 75,
  "state": "P"
}
```

Seizure:

```json
{
  "patientId": "demo-patient",
  "deviceId": "manual-test",
  "eeg": [610, 548],
  "ecg": 64,
  "emg": 220,
  "acc": [2.1, -1.4, 980],
  "state": "S"
}
```

## Flutter Dio Example

```dart
final dio = Dio(BaseOptions(
  baseUrl: const String.fromEnvironment(
    'EPICARE_API_BASE_URL',
    defaultValue: 'http://localhost:5000',
  ),
));

final response = await dio.get('/api/patients/demo-patient/latest');
final data = response.data as Map<String, dynamic>;
final state = data['state'] as int;
```

Compact latest endpoint:

```dart
final response = await dio.get('/api/seizure/latest');
final data = response.data as Map<String, dynamic>;
final prediction = data['prediction'] as Map<String, dynamic>;
final commandCode = prediction['code'] as String; // N, P, or S
```

## Realtime Later

SignalR hub:

```text
/hubs/patient-monitoring
```

Events:

```text
patientStateUpdated
deviceCommand
```

For the first Flutter version, polling `GET /api/patients/demo-patient/latest` every 2-5 seconds is enough.
