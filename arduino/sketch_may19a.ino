/****************************************************
 * EPICARE SYSTEM - FINAL COMPLETE CODE
 * ACC بيظهر بس في Seizure
 ****************************************************/

const int ECG_PIN = A0;
const int EMG_PIN = A5;
const int LED_PIN = 4;
const int MOTOR_PIN = 2;
const bool SEND_DEMO_STATE = false;

char systemState = 'N';
char deviceCommand = 'N';
bool hasBackendCommand = false;

float eegPhase1 = 0, eegPhase2 = 0;
unsigned long lastSendTime = 0;
unsigned long lastBlink = 0;
bool blinkState = false;

// متغيرات الـ Accelerometer (تستخدم بس في Seizure)
float accX = 0, accY = 0, accZ = 0;
unsigned long lastAccChange = 0;

// متغيرات للزيادة التدريجية
float ecgValue = 75;
float emgValue = 20;

void setup() {
  Serial.begin(115200);
  pinMode(LED_PIN, OUTPUT);
  pinMode(MOTOR_PIN, OUTPUT);
  digitalWrite(LED_PIN, LOW);
  digitalWrite(MOTOR_PIN, LOW);
}

void loop() {
  unsigned long currentTime = millis();
  readBackendCommand();
  unsigned long cycleTime = currentTime % 15000;  // 15 ثانية لكل loop كامل (5 ثواني لكل حالة)

  // ================= تحديد الحالة =================
  if (cycleTime < 5000) {
    systemState = 'N';   // Normal 0-5 ثواني
    // ECG و EMG طبيعيين
    ecgValue = 72 + random(-3, 3);
    emgValue = 15 + random(-5, 10);
    updateNormalAccelerometer();
  }
  else if (cycleTime < 10000) {
    systemState = 'P';   // Prediction 5-10 ثواني
    // زيادة تدريجية
    float progress = (cycleTime - 5000) / 5000.0;
    ecgValue = 75 + (progress * 20);
    emgValue = 20 + (progress * 60);
    ecgValue += random(-2, 2);
    emgValue += random(-5, 10);
    updateNormalAccelerometer();
  }
  else {
    systemState = 'S';   // Seizure 10-15 ثانية
    // زيادة تدريجية في EMG ونقص في ECG
    float progress = (cycleTime - 10000) / 5000.0;
    ecgValue = 95 - (progress * 35);
    emgValue = 80 + (progress * 150);
    ecgValue += random(-2, 2);
    emgValue += random(-10, 20);
    
    ecgValue = constrain(ecgValue, 55, 100);
    emgValue = constrain(emgValue, 60, 280);
    
    // ================= ACC يشتغل بس هنا =================
    updateAccelerometer(currentTime, cycleTime);
  }

  // ================= إرسال البيانات =================
  if (currentTime - lastSendTime >= 100) {
    lastSendTime = currentTime;
    sendSensorData();
  }

  // ================= تنفيذ المؤثرات =================
  executeActuation();
}

void readBackendCommand() {
  while (Serial.available() > 0) {
    char incoming = Serial.read();
    if (incoming == 'N' || incoming == 'P' || incoming == 'S') {
      deviceCommand = incoming;
      hasBackendCommand = true;
    }
  }
}

// =================  الـ ACC فقط في Seizure =================
void updateAccelerometer(unsigned long currentTime, unsigned long cycleTime) {
  // حركة رعشة بأرقام واقعية
  float progress = (cycleTime - 10000) / 5000.0;
  float intensity = 2.0 + (progress * 3.0);  // 2 to 5
  
  accX = random(-150, 150) / 100.0 * intensity;
  accY = random(-120, 180) / 100.0 * intensity;
  accZ = 980 + random(-80, 100) * intensity;
  
  // رعشة سريعة كل 50ms
  if (currentTime - lastAccChange > 50) {
    lastAccChange = currentTime;
    accX += random(-20, 20) / 100.0;
    accY += random(-20, 20) / 100.0;
    accZ += random(-15, 15);
  }
  
  accX = constrain(accX, -3.5, 3.5);
  accY = constrain(accY, -3.0, 4.0);
  accZ = constrain(accZ, 900, 1080);
}

void updateNormalAccelerometer() {
  accX = random(-20, 21) / 100.0;
  accY = random(-20, 21) / 100.0;
  accZ = 980 + random(-5, 6);
}

// ================= 📡 إرسال البيانات الخام =================
void sendSensorData() {
  int eeg1 = generateEEG(1);
  int eeg2 = generateEEG(2);
  int ecg = (int)ecgValue;
  int emg = (int)emgValue;

  Serial.print("{\"eeg\":[");
  Serial.print(eeg1); Serial.print(",");
  Serial.print(eeg2); Serial.print("],\"ecg\":");
  Serial.print(ecg); Serial.print(",\"emg\":");
  Serial.print(emg); Serial.print(",\"acc\":[");
  Serial.print(accX, 2); Serial.print(",");
  Serial.print(accY, 2); Serial.print(",");
  Serial.print(accZ, 0);

  if (SEND_DEMO_STATE) {
    Serial.print("],\"state\":\"");
    Serial.print(systemState); Serial.println("\"}");
  }
  else {
    Serial.println("]}");
  }
}

// ================= توليد EEG =================
int generateEEG(int channel) {
  float& phase = (channel == 1) ? eegPhase1 : eegPhase2;
  phase += 0.08;
  if (phase > 6.28) phase = 0;
  
  int base = 500 + (int)(sin(phase) * 40) + random(-5, 5);
  
  if (systemState == 'N') {
    return constrain(base, 450, 550);
  }
  else if (systemState == 'P') {
    return constrain(base + random(-10, 15), 430, 570);
  }
  else {
    if (random(0, 100) > 85) {
      base += random(60, 120);
    }
    return constrain(base + random(-15, 20), 400, 650);
  }
}

// ================= ⚙️ تنفيذ المؤثرات =================
void executeActuation() {
  char actuationState = hasBackendCommand ? deviceCommand : systemState;

  if (actuationState == 'N') {
    // 🟢 Normal: LED طافي - موتور واقف
    digitalWrite(LED_PIN, LOW);
    digitalWrite(MOTOR_PIN, LOW);
  }
  else if (actuationState == 'P') {
    // 🟡 Prediction: LED يومض بسرعة - موتور يتحرك
    if (millis() - lastBlink >= 150) {  // وميض سريع كل 150ms
      lastBlink = millis();
      blinkState = !blinkState;
      digitalWrite(LED_PIN, blinkState);
    }
    digitalWrite(MOTOR_PIN, HIGH);  // موتور يبدأ يتحرك
  }
  else {
    // 🔴 Seizure: LED ثابت منور - موتور أقصى سرعة
    digitalWrite(LED_PIN, HIGH);
    digitalWrite(MOTOR_PIN, HIGH);
  }
}
