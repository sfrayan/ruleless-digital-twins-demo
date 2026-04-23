#include "OfficeTwin_FMU.h"

// include fmu header files, typedefs and macros
#include <stdio.h>
#include <string.h>
#include <assert.h>
#include "openmodelica.h"
#include "openmodelica_func.h"
#include "util/omc_error.h"
#include "OfficeTwin_functions.h"

#include "simulation/solver/events.h"

// Set values for all variables that define a start value
OMC_DISABLE_OPT
void setDefaultStartValues(ModelInstance *comp) {
  put_real_element(16.7, 0, &comp->fmuData->modelData->realVarsData[0].attribute.start);
  put_real_element(0.962, 0, &comp->fmuData->modelData->realVarsData[1].attribute.start);
  put_real_element(0, 0, &comp->fmuData->modelData->realVarsData[2].attribute.start);
  put_real_element(0, 0, &comp->fmuData->modelData->realVarsData[3].attribute.start);
  put_real_element(0, 0, &comp->fmuData->modelData->realVarsData[4].attribute.start);
  put_real_element(0, 0, &comp->fmuData->modelData->realVarsData[5].attribute.start);
  put_real_element(0, 0, &comp->fmuData->modelData->realVarsData[6].attribute.start);
  put_real_element(0, 0, &comp->fmuData->modelData->realVarsData[7].attribute.start);
  put_real_element(0, 0, &comp->fmuData->modelData->realVarsData[8].attribute.start);
  put_real_element(0, 0, &comp->fmuData->modelData->realVarsData[9].attribute.start);
  put_real_element(0, 0, &comp->fmuData->modelData->realVarsData[10].attribute.start);
  put_real_element(0, 0, &comp->fmuData->modelData->realVarsData[11].attribute.start);
  put_real_element(0, 0, &comp->fmuData->modelData->realVarsData[12].attribute.start);
  put_real_element(0, 0, &comp->fmuData->modelData->realVarsData[13].attribute.start);
  put_real_element(1085.0, 0, &comp->fmuData->modelData->realVarsData[14].attribute.start);
  put_real_element(16.7, 0, &comp->fmuData->modelData->realVarsData[15].attribute.start);
  put_real_element(0, 0, &comp->fmuData->modelData->realVarsData[16].attribute.start);
  comp->fmuData->modelData->integerVarsData[0].attribute.start = 0;
  comp->fmuData->modelData->integerVarsData[1].attribute.start = 0;
  comp->fmuData->modelData->integerVarsData[2].attribute.start = 0;
  put_real_element(1500.0, 0, &comp->fmuData->modelData->realParameterData[0].attribute.start);
  put_real_element(2000.0, 0, &comp->fmuData->modelData->realParameterData[1].attribute.start);
  put_real_element(0.0, 0, &comp->fmuData->modelData->realParameterData[2].attribute.start);
  put_real_element(350.0, 0, &comp->fmuData->modelData->realParameterData[3].attribute.start);
  put_real_element(80.0, 0, &comp->fmuData->modelData->realParameterData[4].attribute.start);
  put_real_element(-5.0, 0, &comp->fmuData->modelData->realParameterData[5].attribute.start);
  put_real_element(50.0, 0, &comp->fmuData->modelData->realParameterData[6].attribute.start);
  put_real_element(54.0, 0, &comp->fmuData->modelData->realParameterData[7].attribute.start);
  put_real_element(0.0, 0, &comp->fmuData->modelData->realParameterData[8].attribute.start);
  put_real_element(1005.0, 0, &comp->fmuData->modelData->realParameterData[9].attribute.start);
  put_real_element(0.0, 0, &comp->fmuData->modelData->realParameterData[10].attribute.start);
  put_real_element(0.05, 0, &comp->fmuData->modelData->realParameterData[11].attribute.start);
  put_real_element(0.0139, 0, &comp->fmuData->modelData->realParameterData[12].attribute.start);
  put_real_element(0.0, 0, &comp->fmuData->modelData->realParameterData[13].attribute.start);
  put_real_element(0.5, 0, &comp->fmuData->modelData->realParameterData[14].attribute.start);
  put_real_element(1.2, 0, &comp->fmuData->modelData->realParameterData[15].attribute.start);
}
// Set values for all variables that define a start value
OMC_DISABLE_OPT
void setStartValues(ModelInstance *comp) {
  put_real_element(comp->fmuData->localData[0]->realVars[0], 0, &comp->fmuData->modelData->realVarsData[0].attribute.start);
  put_real_element(comp->fmuData->localData[0]->realVars[1], 0, &comp->fmuData->modelData->realVarsData[1].attribute.start);
  put_real_element(comp->fmuData->localData[0]->realVars[2], 0, &comp->fmuData->modelData->realVarsData[2].attribute.start);
  put_real_element(comp->fmuData->localData[0]->realVars[3], 0, &comp->fmuData->modelData->realVarsData[3].attribute.start);
  put_real_element(comp->fmuData->localData[0]->realVars[4], 0, &comp->fmuData->modelData->realVarsData[4].attribute.start);
  put_real_element(comp->fmuData->localData[0]->realVars[5], 0, &comp->fmuData->modelData->realVarsData[5].attribute.start);
  put_real_element(comp->fmuData->localData[0]->realVars[6], 0, &comp->fmuData->modelData->realVarsData[6].attribute.start);
  put_real_element(comp->fmuData->localData[0]->realVars[7], 0, &comp->fmuData->modelData->realVarsData[7].attribute.start);
  put_real_element(comp->fmuData->localData[0]->realVars[8], 0, &comp->fmuData->modelData->realVarsData[8].attribute.start);
  put_real_element(comp->fmuData->localData[0]->realVars[9], 0, &comp->fmuData->modelData->realVarsData[9].attribute.start);
  put_real_element(comp->fmuData->localData[0]->realVars[10], 0, &comp->fmuData->modelData->realVarsData[10].attribute.start);
  put_real_element(comp->fmuData->localData[0]->realVars[11], 0, &comp->fmuData->modelData->realVarsData[11].attribute.start);
  put_real_element(comp->fmuData->localData[0]->realVars[12], 0, &comp->fmuData->modelData->realVarsData[12].attribute.start);
  put_real_element(comp->fmuData->localData[0]->realVars[13], 0, &comp->fmuData->modelData->realVarsData[13].attribute.start);
  put_real_element(comp->fmuData->localData[0]->realVars[14], 0, &comp->fmuData->modelData->realVarsData[14].attribute.start);
  put_real_element(comp->fmuData->localData[0]->realVars[15], 0, &comp->fmuData->modelData->realVarsData[15].attribute.start);
  put_real_element(comp->fmuData->localData[0]->realVars[16], 0, &comp->fmuData->modelData->realVarsData[16].attribute.start);
  comp->fmuData->modelData->integerVarsData[0].attribute.start = comp->fmuData->localData[0]->integerVars[0];
  comp->fmuData->modelData->integerVarsData[1].attribute.start = comp->fmuData->localData[0]->integerVars[1];
  comp->fmuData->modelData->integerVarsData[2].attribute.start = comp->fmuData->localData[0]->integerVars[2];
  put_real_element(comp->fmuData->simulationInfo->realParameter[0], 0, &comp->fmuData->modelData->realParameterData[0].attribute.start);
  put_real_element(comp->fmuData->simulationInfo->realParameter[1], 0, &comp->fmuData->modelData->realParameterData[1].attribute.start);
  put_real_element(comp->fmuData->simulationInfo->realParameter[2], 0, &comp->fmuData->modelData->realParameterData[2].attribute.start);
  put_real_element(comp->fmuData->simulationInfo->realParameter[3], 0, &comp->fmuData->modelData->realParameterData[3].attribute.start);
  put_real_element(comp->fmuData->simulationInfo->realParameter[4], 0, &comp->fmuData->modelData->realParameterData[4].attribute.start);
  put_real_element(comp->fmuData->simulationInfo->realParameter[5], 0, &comp->fmuData->modelData->realParameterData[5].attribute.start);
  put_real_element(comp->fmuData->simulationInfo->realParameter[6], 0, &comp->fmuData->modelData->realParameterData[6].attribute.start);
  put_real_element(comp->fmuData->simulationInfo->realParameter[7], 0, &comp->fmuData->modelData->realParameterData[7].attribute.start);
  put_real_element(comp->fmuData->simulationInfo->realParameter[8], 0, &comp->fmuData->modelData->realParameterData[8].attribute.start);
  put_real_element(comp->fmuData->simulationInfo->realParameter[9], 0, &comp->fmuData->modelData->realParameterData[9].attribute.start);
  put_real_element(comp->fmuData->simulationInfo->realParameter[10], 0, &comp->fmuData->modelData->realParameterData[10].attribute.start);
  put_real_element(comp->fmuData->simulationInfo->realParameter[11], 0, &comp->fmuData->modelData->realParameterData[11].attribute.start);
  put_real_element(comp->fmuData->simulationInfo->realParameter[12], 0, &comp->fmuData->modelData->realParameterData[12].attribute.start);
  put_real_element(comp->fmuData->simulationInfo->realParameter[13], 0, &comp->fmuData->modelData->realParameterData[13].attribute.start);
  put_real_element(comp->fmuData->simulationInfo->realParameter[14], 0, &comp->fmuData->modelData->realParameterData[14].attribute.start);
  put_real_element(comp->fmuData->simulationInfo->realParameter[15], 0, &comp->fmuData->modelData->realParameterData[15].attribute.start);
}


// implementation of the Model Exchange functions
// Used to set the next time event, if any.
void eventUpdate(ModelInstance* comp, fmi2EventInfo* eventInfo) {
}

static const int realAliasIndexes[1] = {
  0
};

fmi2Real getReal(ModelInstance* comp, const fmi2ValueReference vr) {
  if (vr < 17) {
    return comp->fmuData->localData[0]->realVars[vr];
  }
  if (vr < 33) {
    return comp->fmuData->simulationInfo->realParameter[vr-17];
  }
  if (vr < 34) {
    int ix = realAliasIndexes[vr-33];
    return ix>=0 ? getReal(comp, ix) : -getReal(comp, -(ix+1));
  }
  return NAN;
}

fmi2Status setReal(ModelInstance* comp, const fmi2ValueReference vr, const fmi2Real value) {
  // set start value attribute for all variable that has start value, till initialization mode
  if (vr < 17 && (comp->state == model_state_instantiated || comp->state == model_state_initialization_mode)) {
    put_real_element(value, 0, &comp->fmuData->modelData->realVarsData[vr].attribute.start);
  }
  if (vr < 17) {
    comp->fmuData->localData[0]->realVars[vr] = value;
    return fmi2OK;
  }
  if (vr < 33) {
    comp->fmuData->simulationInfo->realParameter[vr-17] = value;
    return fmi2OK;
  }
  if (vr < 34) {
    int ix = realAliasIndexes[vr-33];
    return ix >= 0 ? setReal(comp, ix, value) : setReal(comp, -(ix+1), -value);
  }
  return fmi2Error;
}

fmi2Integer getInteger(ModelInstance* comp, const fmi2ValueReference vr) {
  if (vr < 3) {
    return comp->fmuData->localData[0]->integerVars[vr];
  }
  if (vr < 3) {
    return comp->fmuData->simulationInfo->integerParameter[vr-3];
  }
  return 0;
}

fmi2Status setInteger(ModelInstance* comp, const fmi2ValueReference vr, const fmi2Integer value) {
  // set start value attribute for all variable that has start value, till initialization mode
  if (vr < 3 && (comp->state == model_state_instantiated || comp->state == model_state_initialization_mode)) {
    comp->fmuData->modelData->integerVarsData[vr].attribute.start = value;
  }
  if (vr < 3) {
    comp->fmuData->localData[0]->integerVars[vr] = value;
    return fmi2OK;
  }
  if (vr < 3) {
    comp->fmuData->simulationInfo->integerParameter[vr-3] = value;
    return fmi2OK;
  }
  return fmi2Error;
}
fmi2Boolean getBoolean(ModelInstance* comp, const fmi2ValueReference vr) {
  switch (vr) {
    default:
      return fmi2False;
  }
}

fmi2Status setBoolean(ModelInstance* comp, const fmi2ValueReference vr, const fmi2Boolean value) {
  switch (vr) {
    default:
      return fmi2Error;
  }
  return fmi2OK;
}

fmi2String getString(ModelInstance* comp, const fmi2ValueReference vr) {
  switch (vr) {
    default:
      return "";
  }
}

fmi2Status setString(ModelInstance* comp, const fmi2ValueReference vr, fmi2String value) {
  switch (vr) {
    default:
      return fmi2Error;
  }
  return fmi2OK;
}

fmi2Status setExternalFunction(ModelInstance* c, const fmi2ValueReference vr, const void* value){
  switch (vr) {
    default:
      return fmi2Error;
  }
  return fmi2OK;
}

/* function maps input references to a input index used in partialDerivatives */
fmi2ValueReference mapInputReference2InputNumber(const fmi2ValueReference vr) {
    switch (vr) {
      default:
        return -1;
    }
}
/* function maps output references to a input index used in partialDerivatives */
fmi2ValueReference mapOutputReference2OutputNumber(const fmi2ValueReference vr) {
    switch (vr) {
      case 11: return 0; break;
      case 15: return 1; break;
      default:
        return -1;
    }
}
/* function maps output references to an internal output Real derivatives */
fmi2ValueReference mapOutputReference2RealOutputDerivatives(const fmi2ValueReference vr) {
    switch (vr) {
      case 11: return 7; break;
      case 15: return 8; break;
      default:
        return -1;
    }
}
/* function maps initialUnknowns UnknownVars ValueReferences to an internal partial derivatives index */
fmi2ValueReference mapInitialUnknownsdependentIndex(const fmi2ValueReference vr) {
    switch (vr) {
      case 2: return 0; break;
      case 3: return 1; break;
      case 11: return 2; break;
      case 15: return 3; break;
      case 19: return 4; break;
      case 25: return 5; break;
      case 27: return 6; break;
      case 30: return 7; break;
      default:
        return -1;
    }
}
/* function maps initialUnknowns knownVars ValueReferences to an internal partial derivatives index */
fmi2ValueReference mapInitialUnknownsIndependentIndex(const fmi2ValueReference vr) {
    switch (vr) {
      case 0: return 0; break;
      case 1: return 1; break;
      case 17: return 2; break;
      case 18: return 3; break;
      case 20: return 4; break;
      case 21: return 5; break;
      case 22: return 6; break;
      case 23: return 7; break;
      case 24: return 8; break;
      case 26: return 9; break;
      case 28: return 10; break;
      case 29: return 11; break;
      case 31: return 12; break;
      case 32: return 13; break;
      default:
        return -1;
    }
}

