#ifndef SECRETS_H
#define SECRETS_H

// WiFi Credentials
#define WIFI_SSID "REPLACE_WITH_YOUR_WIFI_SSID"
#define WIFI_PASSWORD "REPLACE_WITH_YOUR_WIFI_PASSWORD"

// MQTT Credentials (TLS/SSL)
#define MQTT_HOST "mqtt.hydroespinaca.online"
#define MQTT_PORT 8883
#define MQTT_USER "REPLACE_WITH_MQTT_USER"
#define MQTT_PASSWD "REPLACE_WITH_MQTT_PASSWORD"

// MQTT TLS Certificate (ISRG Root X1 from Let's Encrypt)
static const char MQTT_ROOT_CA[] PROGMEM = R"EOF(
-----BEGIN CERTIFICATE-----
PASTE_ISRG_ROOT_X1_CERTIFICATE_HERE
-----END CERTIFICATE-----
)EOF";

#endif
