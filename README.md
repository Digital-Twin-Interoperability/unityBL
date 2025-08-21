# Kafka-HTTP-Unity Setup Guide

This guide walks through setting up Kafka, the HTTP bridge, and Unity for running the **Kafka-HTTP-Unity** integration.

---

## 1. Prerequisites

* **Docker Desktop** installed and running.
* **Node.js** installed.
* **Unity Hub** + project with the `unitybl` scene.

---

## 2. Start Kafka and Services

### Step 1: Open Docker Desktop

Make sure Docker Desktop is running.

### Step 2: Start Zookeeper

```powershell
# PowerShell
cd C:\Users\reqal\Downloads\kafka_2.13-4.0.0

docker run -d --name zookeeper --network kafka-network -p 2181:2181 \
  -e ZOOKEEPER_CLIENT_PORT=2181 \
  -e ZOOKEEPER_TICK_TIME=2000 \
  confluentinc/cp-zookeeper:7.4.0
```

Wait **\~30 seconds**.

### Step 3: Start Kafka Broker

```powershell
docker run -d --name kafka --network kafka-network -p 9092:9092 \
  -e KAFKA_BROKER_ID=1 \
  -e KAFKA_ZOOKEEPER_CONNECT=zookeeper:2181 \
  -e KAFKA_ADVERTISED_LISTENERS=INTERNAL://kafka:29092,EXTERNAL://192.168.1.74:9092 \
  -e KAFKA_LISTENERS=INTERNAL://0.0.0.0:29092,EXTERNAL://0.0.0.0:9092 \
  -e KAFKA_LISTENER_SECURITY_PROTOCOL_MAP=INTERNAL:PLAINTEXT,EXTERNAL:PLAINTEXT \
  -e KAFKA_INTER_BROKER_LISTENER_NAME=INTERNAL \
  -e KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR=1 \
  confluentinc/cp-kafka:7.4.0
```

Wait **\~15 seconds**.

### Step 4: Start Kafka UI

```powershell
docker run -d --name kafka-ui --network kafka-network -p 8080:8080 \
  -e KAFKA_CLUSTERS_0_NAME=local \
  -e KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS=kafka:29092 \
  provectuslabs/kafka-ui:latest
```

### Step 5: Verify Kafka UI

* Open **[http://localhost:8080](http://localhost:8080)** in your browser.
* Check that:

  * The **broker** is active.
  * Required topics exist (`omniverse-position-data`, `hsml-data`, `agent-position-updates`, `movement-commands`).
  * Consumers include `omniverse-actiongraph-consumer` and `unity-http-consumer`.

If topics are missing, create them with:

```powershell
docker exec kafka kafka-topics --bootstrap-server kafka:29092 --create --topic <topic-name> --partitions 1 --replication-factor 1
```

---

## 3. Start the Unity HTTP Bridge

### Step 1: Open Command Prompt (not PowerShell)

```cmd
cd C:\Users\reqal\Downloads\kafka_2.13-4.0.0\kafka_2.13-4.0.0\unity-http-bridge
```

### Step 2: Install dependencies

```cmd
npm init -y
npm install express kafkajs cors
```

### Step 3: Run the bridge

```cmd
node server.js
```

> ⚠️ If errors appear, that’s okay—this just means the backend network isn’t fully wired yet.

### Step 4: Check IP address

Make sure the **IP in `server.js`** matches your local machine:

```cmd
ipconfig
```

If different from `192.168.1.74`, update the IP in:

```
C:\Users\reqal\Downloads\kafka_2.13-4.0.0\kafka_2.13-4.0.0\unity-http-bridge\server.js
```

---

## 4. Unity Setup

### Step 1: Open Project

* Open Unity Hub.
* Launch the project and load the **`unitybl` scene**.

### Step 2: Check KafkaManager GameObject

* Ensure `KafkaManager` has active scripts:

  * **Kafka Producer (Script)**
  * **Kafka Consumer (Script)**

### Step 3: Producer Setup

* 3D Rover Prefab should include:

  * `Mover` script
  * `Producer` script
  * `Mesh Renderer`
  * `NavMesh Agent`

### Step 4: Consumer Setup

* Consumer object should include:

  * Rover prefab
  * Meshes
  * `Box Collider`
  * Consumer logic script

### Step 5: Play Mode

* Hit **Play** in Unity.
* Confirm:

  * Messages are produced and consumed via Kafka.
  * Positions update correctly from the rover prefab.

---

## 5. Troubleshooting

* If Kafka/Docker isn’t working:

  * Stop and remove containers:

    ```powershell
    ```

docker stop kafka-ui kafka zookeeper
docker rm kafka-ui kafka zookeeper
\`\`\`

* Recreate the Docker network (only if broken):

  ```powershell
  ```

docker network rm kafka-network
docker network create kafka-network
\`\`\`

* Restart services following steps above.

---

