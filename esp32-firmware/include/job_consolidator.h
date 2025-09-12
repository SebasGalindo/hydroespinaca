#pragma once

#include <Arduino.h>
#include <vector>
#include "job_scheduler.h"

class JobConsolidator {
public:
    static bool canConsolidate(const Job& incoming, const Job& current);
    static void consolidateJob(Job& current, const Job& incoming, std::vector<String>& logs);
    static bool isControlRoutine(const Job& job);
};