#include "job_utils.h"
#include <NTPClient.h>
#include <time.h>

// External NTPClient instance from main.cpp
extern NTPClient timeClient;

String JobUtils::getCurrentTimestamp() {
    if (!timeClient.isTimeSet()) {
        timeClient.forceUpdate();
    }
    
    unsigned long epochTime = timeClient.getEpochTime();
    
    // Convert to tm struct for formatting
    time_t rawtime = epochTime;
    struct tm * timeinfo = gmtime(&rawtime);
    
    char isoBuffer[32];
    strftime(isoBuffer, sizeof(isoBuffer), "%Y-%m-%dT%H:%M:%SZ", timeinfo);
    
    return String(isoBuffer);
}