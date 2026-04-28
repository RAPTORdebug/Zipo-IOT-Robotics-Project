// ZIPO Code

// Libraries
#include <vector>
#include <WiFi.h>
#include <Wire.h>
#include <Adafruit_Sensor.h>
#include <Adafruit_MPU6050.h>
#include <Firebase_ESP_Client.h>

// Defining all the pins
// Motor Driver
#define ena 14
#define in1 27
#define in2 26
#define in3 25
#define in4 33
#define enb 32

// IR Sensors
#define irl 23
#define irr 13

// Ultrasonic
#define trig 18
#define echo 19

// Gyro
#define gyrosda 21
#define gyroscl 22

// Firebase and WiFi Creds
#define WIFI_SSID "SM-THINKPAD"
#define WIFI_PASSWORD "S20071210"
#define API_KEY "AIzaSyC7ZOk2qezY9Igh2VRxa1dYsxCtzBwL1o8"
#define DATABASE_URL "https://zipo-32d48-default-rtdb.asia-southeast1.firebasedatabase.app"

// Gyro Setup
Adafruit_MPU6050 mpu;

// Firebase Setup
FirebaseData fbdo;
FirebaseAuth auth;
FirebaseConfig config;

// Straight Movement Variables
int speed = 200;
int turnSpeed = 200;
int ticksPerCell = 0;
bool counting = false;
volatile int irlCount = 0; // IR Left
volatile int irrCount = 0; // IR Right

// Rotation Variables
float targetAngleZ = 0;
float angleZ = 0;
unsigned long lastGyroTime;
float offsetZ = 0;
bool handled = false;

// Obstacle Detection
int cellDistance;
bool obstacleHandled = false;

// Drive command
bool drive = false;

// Position Data
int x = 0;
int y = 0;
int dir = 0;

// Path to drive
std::vector<int> path;

// IR Counter Functions
void IRAM_ATTR countLeft() {
  irlCount++;
}

void IRAM_ATTR countRight() {
  irrCount++;
}

void setup(){
  // Firebase Log In Creds
  auth.user.email = "tester@zipo.com";
  auth.user.password = "zipotester";

  // Starting processes
  Serial.begin(115200);
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
  Serial.println("Wi-Fi Connected");
  mpu.begin();
  
  // Connecting to Firebase
  config.api_key = API_KEY;
  config.database_url = DATABASE_URL;
  Firebase.begin(&config, &auth);
  Firebase.reconnectWiFi(true);

  // Set Pins Mode
  pinMode(ena, OUTPUT);
  pinMode(in1, OUTPUT);
  pinMode(in2, OUTPUT);
  pinMode(in3, OUTPUT);
  pinMode(in4, OUTPUT);
  pinMode(enb, OUTPUT);

  pinMode(irl, INPUT_PULLUP);
  pinMode(irr, INPUT_PULLUP);
  
  pinMode(trig, OUTPUT);
  pinMode(echo, INPUT);

  // IR Interupts (Added Delay for Stability)
  delay(1000);
  attachInterrupt(digitalPinToInterrupt(irl), countLeft, CHANGE);
  attachInterrupt(digitalPinToInterrupt(irr), countRight, CHANGE);

  // Setup Gyro
  mpu.setGyroRange(MPU6050_RANGE_250_DEG);
  lastGyroTime = millis();

  // Reduces Gyro Inaccuracy
  calibrateGyro();

  // Get Ticks Per Cell and Target Angle Z
  readFirebase();
  getPositionDataFromFirebase();

}

void loop(){
  // Checks for "drive" boolean to see if the car needs to follow the path in database
  if (Firebase.RTDB.getBool(&fbdo, "/car/drive")){
    drive = fbdo.boolData();
  }
  
  // Checks for "update" boolean to see if the code variables should be updated
  if (Firebase.RTDB.getBool(&fbdo, "/car/update")){
    if (fbdo.boolData()){
      readFirebase();
      getPositionDataFromFirebase();
      Firebase.RTDB.setBool(&fbdo, "/car/update", false);
    }
  }

  if (drive) {
    // This gets the path in the database and add it to the list (vector)
    if (Firebase.RTDB.getArray(&fbdo, "/car/path")) {
      path.clear();

      // We got a path
      FirebaseJsonArray &arr = fbdo.jsonArray();
      size_t len = arr.size();

      // Copy the path
      for (size_t i = 0; i < len; i++) {

        FirebaseJsonData result;
        arr.get(result, i);

        if (result.success) {
          path.push_back(result.to<int>());
        }
      }
    }

    // Follow path now
    if (!path.empty()){
      Serial.println("Following Path");
      for (int step : path) {
        // Checks for an obstacle in front
        if (isObstacleClose(cellDistance) && !obstacleHandled){
          Firebase.RTDB.setBool(&fbdo, "/car/obstacleInFront", true);
          Serial.println("Obstacled Detected");
          // This is added because it goes to an infinite loop if the obstacle isn't removed
          obstacleHandled = true;
          path.clear();
          // Stops following the path when there is an obstacle
          break;
        }

        handled = false;

        // The while is used so the code doesn't do anything else while the car is moving
        while (!moveCar(step)){}

        obstacleHandled = false;

        updatePositionInFirebase();

        // If there is something in front do something
        delay(100);
      }
    }

    // Set Drive to False
    Firebase.RTDB.setBool(&fbdo, "/car/drive", false);
    drive = false;
  }
}

bool isObstacleClose(int distanceThresholdCM) {
  // Trigger pulse
  digitalWrite(trig, LOW);
  delayMicroseconds(2);

  digitalWrite(trig, HIGH);
  delayMicroseconds(10);
  digitalWrite(trig, LOW);

  // Read echo time
  long duration = pulseIn(echo, HIGH, 30000); // timeout 30ms

  // If no signal
  if (duration == 0) return false;

  // Convert to distance (cm)
  float distance = duration * 0.0343 / 2;

  return distance <= distanceThresholdCM;
}

// Get Ticks Per Cell, Target Angle Z and Cell Distance
void readFirebase(){
  if (Firebase.RTDB.getInt(&fbdo, "/car/ticksPerCell")){
    ticksPerCell = fbdo.intData();
  }
  else{
    ticksPerCell = 0;
    Firebase.RTDB.setString(&fbdo, "/car/errorString", "Didn't recieve ticksPerCell");
  }

  if (Firebase.RTDB.getInt(&fbdo, "/car/targetAngleZ")){
    targetAngleZ = fbdo.intData();
  }
  else{
    targetAngleZ = 0;
    Firebase.RTDB.setString(&fbdo, "/car/errorString", "Didn't recieve targetAngleZ");
  }

  if (Firebase.RTDB.getInt(&fbdo, "/car/cellDistance")){
    cellDistance = fbdo.intData();
  }
  else{
    cellDistance = 0;
    Firebase.RTDB.setString(&fbdo, "/car/errorString", "Didn't recieve cellDistance");
  }
}

// Give position data to Fireabse
void updatePositionInFirebase(){
  Firebase.RTDB.setInt(&fbdo, "/car/xPos", x);
  Firebase.RTDB.setInt(&fbdo, "/car/yPos", y);
  Firebase.RTDB.setInt(&fbdo, "/car/carDirection", dir);
}

// Get position data from Firebase
void getPositionDataFromFirebase(){
  if (Firebase.RTDB.getInt(&fbdo, "/car/xPos")) {
    x = fbdo.intData();
  }
  if (Firebase.RTDB.getInt(&fbdo, "/car/yPos")) {
    y = fbdo.intData();
  }
  if (Firebase.RTDB.getInt(&fbdo, "/car/carDirection")) {
    dir = fbdo.intData();
  }
}

// 1 - Forward, 2 - Right, 3 - Left
bool moveCar(int moving){

  if (moving == 1){
    // Go Forward
    while (!moveForward());

    counting = false;
    
    return true;
  }

  else if (moving == 2 || moving == 3){
    // Turn
    angleZ = 0;
    lastGyroTime = millis();

    while (!turn(moving));
    
    return true;
  }

  else {
    Serial.println("Invalid Movement");
    return true;
  }

  return false;
}

bool moveForward(){
  // Only set these 0 once.
  if (!counting){
    irlCount = 0;
    irrCount = 0;
    counting = true;
  }

  if (irrCount > ticksPerCell){
    Firebase.RTDB.setInt(&fbdo, "car/ticksCount", irrCount);
    stop();
    // Update position according to direction
    if (dir == 0){
      y += 1;
    }
    else if (dir == 1){
      x += 1;
    }
    else if (dir == 2){
      y -= 1;
    }
    else if (dir == 3){
      x -= 1;
    }
    else{
      Serial.println("There was an issue with direction");
    }
    return true;
  }

  // Motor Inputs and Speeds
  // Right
  digitalWrite(in1, LOW);
  digitalWrite(in2, HIGH);

  analogWrite(ena, speed);

  // Left
  digitalWrite(in3, HIGH);
  digitalWrite(in4, LOW);

  analogWrite(enb, speed);

  return false;
}

void calibrateGyro() {
  Serial.println("Calibrating... DO NOT MOVE");

  // Gets 1000 samples and gets an average offset of the gyro
  
  float sum = 0;
  int samples = 1000;

  for(int i = 0; i < samples; i++) {
    sensors_event_t a, g, temp;
    mpu.getEvent(&a, &g, &temp);
    sum += g.gyro.z;
    delay(2);
  }

  offsetZ = sum / samples;
  Serial.println("Calibration done");
}

// Returns true if the car has turned 90 degrees
bool checkGyro(){
  // a - accelometer, g - gyro, temp - temp which is not used
  sensors_event_t a, g, temp;
  mpu.getEvent(&a, &g, &temp);

  unsigned long now = millis();

  // Gyro gives angular velocity so we take difference in seconds
  float dt = (now - lastGyroTime) / 1000.0;
  lastGyroTime = now;

  // gyroZ converted to degrees because it's radians
  float gyroZ = (g.gyro.z - offsetZ) * 57.2958;

  // Noise Filtering
  if(abs(gyroZ) > 0.5) {  
    angleZ += gyroZ * dt;
  }

  if (abs(angleZ) > targetAngleZ){
    return true;
  }

  return false;
}

// Right = 2 and Left = 3
bool turn(int turnSide){

  // Gyro check and stop then return true.
  if (checkGyro() && !handled){
    stop();
    Firebase.RTDB.setFloat(&fbdo, "/car/angleZ", angleZ);
    handled = true;
    if (turnSide == 2) {        // Right
      dir = (dir + 1) % 4;
    }
    else if (turnSide == 3) {   // Left
      dir = (dir + 3) % 4;  // same as -1 but safe
    }
    return true;
  }
  else{
    // Set Motor Controller Input Values to correct sides.
    if (turnSide == 2){
      // Right
      digitalWrite(in1, LOW);
      digitalWrite(in2, HIGH);

      // Left
      digitalWrite(in3, LOW);
      digitalWrite(in4, HIGH);
    }
    else if (turnSide == 3){
      // Right
      digitalWrite(in1, HIGH);
      digitalWrite(in2, LOW);

      // Left
      digitalWrite(in3, HIGH);
      digitalWrite(in4, LOW);
    }
    else{
      Serial.println("Invalid Turn Side Input");
      return true;
    }

    // Adjust speed
    analogWrite(ena, turnSpeed);
    analogWrite(enb, turnSpeed);
  }

  return false;
}

// Stops all the motors
void stop(){
  // Right
  digitalWrite(in1, LOW);
  digitalWrite(in2, LOW);

  analogWrite(ena, 0);

  // Left
  digitalWrite(in3, LOW);
  digitalWrite(in4, LOW);

  analogWrite(enb, 0);
}