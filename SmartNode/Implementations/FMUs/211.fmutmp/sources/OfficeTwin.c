/* Main Simulation File */

#if defined(__cplusplus)
extern "C" {
#endif

#include "OfficeTwin_model.h"
#include "simulation/solver/events.h"
#include "util/real_array.h"



/* dummy VARINFO and FILEINFO */
const VAR_INFO dummyVAR_INFO = omc_dummyVarInfo;

int OfficeTwin_input_function(DATA *data, threadData_t *threadData)
{
  (data->localData[0]->integerVars[data->simulationInfo->integerVarsIndex[0]] /* DehumidifierActuator variable */) = data->simulationInfo->inputVars[0];
  (data->localData[0]->integerVars[data->simulationInfo->integerVarsIndex[1]] /* FloorHeatingActuator variable */) = data->simulationInfo->inputVars[1];
  (data->localData[0]->integerVars[data->simulationInfo->integerVarsIndex[2]] /* HeaterActuator variable */) = data->simulationInfo->inputVars[2];
  
  return 0;
}

int OfficeTwin_input_function_init(DATA *data, threadData_t *threadData)
{
  data->simulationInfo->inputVars[0] = data->modelData->integerVarsData[0].attribute.start;
  data->simulationInfo->inputVars[1] = data->modelData->integerVarsData[1].attribute.start;
  data->simulationInfo->inputVars[2] = data->modelData->integerVarsData[2].attribute.start;
  
  return 0;
}

int OfficeTwin_input_function_updateStartValues(DATA *data, threadData_t *threadData)
{
  data->modelData->integerVarsData[0].attribute.start = data->simulationInfo->inputVars[0];
  data->modelData->integerVarsData[1].attribute.start = data->simulationInfo->inputVars[1];
  data->modelData->integerVarsData[2].attribute.start = data->simulationInfo->inputVars[2];
  
  return 0;
}

int OfficeTwin_inputNames(DATA *data, char ** names){
  names[0] = (char *) data->modelData->integerVarsData[0].info.name;
  names[1] = (char *) data->modelData->integerVarsData[1].info.name;
  names[2] = (char *) data->modelData->integerVarsData[2].info.name;
  
  return 0;
}

int OfficeTwin_data_function(DATA *data, threadData_t *threadData)
{
  return 0;
}

int OfficeTwin_dataReconciliationInputNames(DATA *data, char ** names){
  
  return 0;
}

int OfficeTwin_dataReconciliationUnmeasuredVariables(DATA *data, char ** names)
{
  
  return 0;
}

int OfficeTwin_output_function(DATA *data, threadData_t *threadData)
{
  data->simulationInfo->outputVars[0] = (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[11]] /* Humidity variable */);
  data->simulationInfo->outputVars[1] = (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[15]] /* Temperature variable */);
  
  return 0;
}

int OfficeTwin_setc_function(DATA *data, threadData_t *threadData)
{
  
  return 0;
}

int OfficeTwin_setb_function(DATA *data, threadData_t *threadData)
{
  
  return 0;
}


/*
equation index: 17
type: SIMPLE_ASSIGN
$DER.W = (m_per + m_vent * (W_out - W) - m_deh * (*Real*)(DehumidifierActuator)) / m_air
*/
void OfficeTwin_eqFunction_17(DATA *data, threadData_t *threadData)
{
  const int equationIndexes[2] = {1,17};
  (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[3]] /* der(W) STATE_DER */) = DIVISION_SIM((data->simulationInfo->realParameter[data->simulationInfo->realParamsIndex[12]] /* m_per PARAM */) + ((data->simulationInfo->realParameter[data->simulationInfo->realParamsIndex[13]] /* m_vent PARAM */)) * ((data->simulationInfo->realParameter[data->simulationInfo->realParamsIndex[8]] /* W_out PARAM */) - (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[1]] /* W STATE(1) */)) - (((data->simulationInfo->realParameter[data->simulationInfo->realParamsIndex[11]] /* m_deh PARAM */)) * (((modelica_real)(data->localData[0]->integerVars[data->simulationInfo->integerVarsIndex[0]] /* DehumidifierActuator variable */)))),(data->simulationInfo->realParameter[data->simulationInfo->realParamsIndex[10]] /* m_air PARAM */),"m_air",equationIndexes);
  threadData->lastEquationSolved = 17;
}

/*
equation index: 18
type: SIMPLE_ASSIGN
Q_loss = UA * ($outputAlias_Temperature - T_out)
*/
void OfficeTwin_eqFunction_18(DATA *data, threadData_t *threadData)
{
  const int equationIndexes[2] = {1,18};
  (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[14]] /* Q_loss variable */) = ((data->simulationInfo->realParameter[data->simulationInfo->realParamsIndex[6]] /* UA PARAM */)) * ((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[0]] /* $outputAlias_Temperature STATE(1,$Temperature_der) */) - (data->simulationInfo->realParameter[data->simulationInfo->realParamsIndex[5]] /* T_out PARAM */));
  threadData->lastEquationSolved = 18;
}

/*
equation index: 19
type: SIMPLE_ASSIGN
Q_heat = P_htr * (*Real*)(HeaterActuator) + P_flr * (*Real*)(FloorHeatingActuator)
*/
void OfficeTwin_eqFunction_19(DATA *data, threadData_t *threadData)
{
  const int equationIndexes[2] = {1,19};
  (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[13]] /* Q_heat variable */) = ((data->simulationInfo->realParameter[data->simulationInfo->realParamsIndex[1]] /* P_htr PARAM */)) * (((modelica_real)(data->localData[0]->integerVars[data->simulationInfo->integerVarsIndex[2]] /* HeaterActuator variable */))) + ((data->simulationInfo->realParameter[data->simulationInfo->realParamsIndex[0]] /* P_flr PARAM */)) * (((modelica_real)(data->localData[0]->integerVars[data->simulationInfo->integerVarsIndex[1]] /* FloorHeatingActuator variable */)));
  threadData->lastEquationSolved = 19;
}

/*
equation index: 20
type: SIMPLE_ASSIGN
$Temperature_der = (Q_heat + Q_int - Q_loss) / (m_air * cp)
*/
void OfficeTwin_eqFunction_20(DATA *data, threadData_t *threadData)
{
  const int equationIndexes[2] = {1,20};
  (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[8]] /* $Temperature_der variable */) = DIVISION_SIM((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[13]] /* Q_heat variable */) + (data->simulationInfo->realParameter[data->simulationInfo->realParamsIndex[3]] /* Q_int PARAM */) - (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[14]] /* Q_loss variable */),((data->simulationInfo->realParameter[data->simulationInfo->realParamsIndex[10]] /* m_air PARAM */)) * ((data->simulationInfo->realParameter[data->simulationInfo->realParamsIndex[9]] /* cp PARAM */)),"m_air * cp",equationIndexes);
  threadData->lastEquationSolved = 20;
}

/*
equation index: 21
type: SIMPLE_ASSIGN
$DER.$outputAlias_Temperature = $Temperature_der
*/
void OfficeTwin_eqFunction_21(DATA *data, threadData_t *threadData)
{
  const int equationIndexes[2] = {1,21};
  (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[2]] /* der($outputAlias_Temperature) STATE_DER */) = (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[8]] /* $Temperature_der variable */);
  threadData->lastEquationSolved = 21;
}

/*
equation index: 22
type: SIMPLE_ASSIGN
Temperature = $outputAlias_Temperature
*/
void OfficeTwin_eqFunction_22(DATA *data, threadData_t *threadData)
{
  const int equationIndexes[2] = {1,22};
  (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[15]] /* Temperature variable */) = (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[0]] /* $outputAlias_Temperature STATE(1,$Temperature_der) */);
  threadData->lastEquationSolved = 22;
}

/*
equation index: 23
type: SIMPLE_ASSIGN
$cse1 = exp(17.27 * $outputAlias_Temperature / ($outputAlias_Temperature + 237.3))
*/
void OfficeTwin_eqFunction_23(DATA *data, threadData_t *threadData)
{
  const int equationIndexes[2] = {1,23};
  (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[9]] /* $cse1 variable */) = exp(DIVISION_SIM((17.27) * ((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[0]] /* $outputAlias_Temperature STATE(1,$Temperature_der) */)),(data->localData[0]->realVars[data->simulationInfo->realVarsIndex[0]] /* $outputAlias_Temperature STATE(1,$Temperature_der) */) + 237.3,"$outputAlias_Temperature + 237.3",equationIndexes));
  threadData->lastEquationSolved = 23;
}

/*
equation index: 24
type: SIMPLE_ASSIGN
Psat_in = 610.78 * $cse1
*/
void OfficeTwin_eqFunction_24(DATA *data, threadData_t *threadData)
{
  const int equationIndexes[2] = {1,24};
  (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[12]] /* Psat_in DUMMY_STATE */) = (610.78) * ((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[9]] /* $cse1 variable */));
  threadData->lastEquationSolved = 24;
}

/*
equation index: 25
type: SIMPLE_ASSIGN
W_sat = 622.0 * Psat_in / (101325.0 - Psat_in)
*/
void OfficeTwin_eqFunction_25(DATA *data, threadData_t *threadData)
{
  const int equationIndexes[2] = {1,25};
  (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[16]] /* W_sat DUMMY_STATE */) = (622.0) * (DIVISION_SIM((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[12]] /* Psat_in DUMMY_STATE */),101325.0 - (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[12]] /* Psat_in DUMMY_STATE */),"101325.0 - Psat_in",equationIndexes));
  threadData->lastEquationSolved = 25;
}

/*
equation index: 26
type: SIMPLE_ASSIGN
Humidity = if W_sat <= 0.0 then 0.0 else if W / W_sat >= 1.0 then 100.0 else if W / W_sat <= 0.0 then 0.0 else 100.0 * W / W_sat
*/
void OfficeTwin_eqFunction_26(DATA *data, threadData_t *threadData)
{
  const int equationIndexes[2] = {1,26};
  modelica_boolean tmp0;
  modelica_real tmp1;
  modelica_real tmp2;
  modelica_boolean tmp3;
  modelica_real tmp4;
  modelica_real tmp5;
  modelica_boolean tmp6;
  modelica_real tmp7;
  modelica_real tmp8;
  modelica_boolean tmp9;
  modelica_real tmp10;
  modelica_boolean tmp11;
  modelica_real tmp12;
  tmp1 = 1.0;
  tmp2 = 0.0;
  relationhysteresis(data, &tmp0, (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[16]] /* W_sat DUMMY_STATE */), 0.0, tmp1, tmp2, 0, LessEq, LessEqZC);
  tmp11 = (modelica_boolean)tmp0;
  if(tmp11)
  {
    tmp12 = 0.0;
  }
  else
  {
    tmp4 = 1.0;
    tmp5 = 1.0;
    relationhysteresis(data, &tmp3, DIVISION_SIM((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[1]] /* W STATE(1) */),(data->localData[0]->realVars[data->simulationInfo->realVarsIndex[16]] /* W_sat DUMMY_STATE */),"W_sat",equationIndexes), 1.0, tmp4, tmp5, 1, GreaterEq, GreaterEqZC);
    tmp9 = (modelica_boolean)tmp3;
    if(tmp9)
    {
      tmp10 = 100.0;
    }
    else
    {
      tmp7 = 1.0;
      tmp8 = 0.0;
      relationhysteresis(data, &tmp6, DIVISION_SIM((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[1]] /* W STATE(1) */),(data->localData[0]->realVars[data->simulationInfo->realVarsIndex[16]] /* W_sat DUMMY_STATE */),"W_sat",equationIndexes), 0.0, tmp7, tmp8, 2, LessEq, LessEqZC);
      tmp10 = (tmp6?0.0:DIVISION_SIM((100.0) * ((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[1]] /* W STATE(1) */)),(data->localData[0]->realVars[data->simulationInfo->realVarsIndex[16]] /* W_sat DUMMY_STATE */),"W_sat",equationIndexes));
    }
    tmp12 = tmp10;
  }
  (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[11]] /* Humidity variable */) = tmp12;
  threadData->lastEquationSolved = 26;
}

/*
equation index: 27
type: SIMPLE_ASSIGN
$outputAlias_Humidity = Humidity
*/
void OfficeTwin_eqFunction_27(DATA *data, threadData_t *threadData)
{
  const int equationIndexes[2] = {1,27};
  (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[10]] /* $outputAlias_Humidity DUMMY_STATE */) = (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[11]] /* Humidity variable */);
  threadData->lastEquationSolved = 27;
}

/*
equation index: 28
type: SIMPLE_ASSIGN
$DER.Psat_in = 10548.1706 * $cse1 * 237.3 * $Temperature_der / (237.3 + $outputAlias_Temperature) ^ 2.0
*/
void OfficeTwin_eqFunction_28(DATA *data, threadData_t *threadData)
{
  const int equationIndexes[2] = {1,28};
  modelica_real tmp13;
  tmp13 = 237.3 + (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[0]] /* $outputAlias_Temperature STATE(1,$Temperature_der) */);
  (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[5]] /* der(Psat_in) DUMMY_DER */) = (10548.1706) * (((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[9]] /* $cse1 variable */)) * ((237.3) * (DIVISION_SIM((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[8]] /* $Temperature_der variable */),(tmp13 * tmp13),"(237.3 + $outputAlias_Temperature) ^ 2.0",equationIndexes))));
  threadData->lastEquationSolved = 28;
}

/*
equation index: 29
type: SIMPLE_ASSIGN
$DER.W_sat = 622.0 * $DER.Psat_in * 101325.0 / (101325.0 - Psat_in) ^ 2.0
*/
void OfficeTwin_eqFunction_29(DATA *data, threadData_t *threadData)
{
  const int equationIndexes[2] = {1,29};
  modelica_real tmp14;
  tmp14 = 101325.0 - (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[12]] /* Psat_in DUMMY_STATE */);
  (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[6]] /* der(W_sat) DUMMY_DER */) = (622.0) * (((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[5]] /* der(Psat_in) DUMMY_DER */)) * (DIVISION_SIM(101325.0,(tmp14 * tmp14),"(101325.0 - Psat_in) ^ 2.0",equationIndexes)));
  threadData->lastEquationSolved = 29;
}

/*
equation index: 30
type: SIMPLE_ASSIGN
$Humidity_der = if W_sat <= 0.0 then 0.0 else if W / W_sat >= 1.0 then 0.0 else if W / W_sat <= 0.0 then 0.0 else (100.0 * der(W) * W_sat - 100.0 * W * $DER.W_sat) / W_sat ^ 2.0
*/
void OfficeTwin_eqFunction_30(DATA *data, threadData_t *threadData)
{
  const int equationIndexes[2] = {1,30};
  modelica_boolean tmp15;
  modelica_real tmp16;
  modelica_real tmp17;
  modelica_boolean tmp18;
  modelica_real tmp19;
  modelica_real tmp20;
  modelica_boolean tmp21;
  modelica_real tmp22;
  modelica_real tmp23;
  modelica_real tmp24;
  modelica_boolean tmp25;
  modelica_real tmp26;
  modelica_boolean tmp27;
  modelica_real tmp28;
  modelica_boolean tmp29;
  modelica_real tmp30;
  tmp16 = 1.0;
  tmp17 = 0.0;
  relationhysteresis(data, &tmp15, (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[16]] /* W_sat DUMMY_STATE */), 0.0, tmp16, tmp17, 0, LessEq, LessEqZC);
  tmp29 = (modelica_boolean)tmp15;
  if(tmp29)
  {
    tmp30 = 0.0;
  }
  else
  {
    tmp19 = 1.0;
    tmp20 = 1.0;
    relationhysteresis(data, &tmp18, DIVISION_SIM((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[1]] /* W STATE(1) */),(data->localData[0]->realVars[data->simulationInfo->realVarsIndex[16]] /* W_sat DUMMY_STATE */),"W_sat",equationIndexes), 1.0, tmp19, tmp20, 1, GreaterEq, GreaterEqZC);
    tmp27 = (modelica_boolean)tmp18;
    if(tmp27)
    {
      tmp28 = 0.0;
    }
    else
    {
      tmp22 = 1.0;
      tmp23 = 0.0;
      relationhysteresis(data, &tmp21, DIVISION_SIM((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[1]] /* W STATE(1) */),(data->localData[0]->realVars[data->simulationInfo->realVarsIndex[16]] /* W_sat DUMMY_STATE */),"W_sat",equationIndexes), 0.0, tmp22, tmp23, 2, LessEq, LessEqZC);
      tmp25 = (modelica_boolean)tmp21;
      if(tmp25)
      {
        tmp26 = 0.0;
      }
      else
      {
        tmp24 = (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[16]] /* W_sat DUMMY_STATE */);
        tmp26 = DIVISION_SIM(((100.0) * ((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[3]] /* der(W) STATE_DER */))) * ((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[16]] /* W_sat DUMMY_STATE */)) - (((100.0) * ((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[1]] /* W STATE(1) */))) * ((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[6]] /* der(W_sat) DUMMY_DER */))),(tmp24 * tmp24),"W_sat ^ 2.0",equationIndexes);
      }
      tmp28 = tmp26;
    }
    tmp30 = tmp28;
  }
  (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[7]] /* $Humidity_der variable */) = tmp30;
  threadData->lastEquationSolved = 30;
}

/*
equation index: 31
type: SIMPLE_ASSIGN
$DER.$outputAlias_Humidity = $Humidity_der
*/
void OfficeTwin_eqFunction_31(DATA *data, threadData_t *threadData)
{
  const int equationIndexes[2] = {1,31};
  (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[4]] /* der($outputAlias_Humidity) DUMMY_DER */) = (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[7]] /* $Humidity_der variable */);
  threadData->lastEquationSolved = 31;
}

OMC_DISABLE_OPT
int OfficeTwin_functionDAE(DATA *data, threadData_t *threadData)
{
  int equationIndexes[1] = {0};
#if !defined(OMC_MINIMAL_RUNTIME)
  if (measure_time_flag) rt_tick(SIM_TIMER_DAE);
#endif

  data->simulationInfo->needToIterate = 0;
  data->simulationInfo->discreteCall = 1;
  OfficeTwin_functionLocalKnownVars(data, threadData);
  static void (*const eqFunctions[15])(DATA*, threadData_t*) = {
    OfficeTwin_eqFunction_17,
    OfficeTwin_eqFunction_18,
    OfficeTwin_eqFunction_19,
    OfficeTwin_eqFunction_20,
    OfficeTwin_eqFunction_21,
    OfficeTwin_eqFunction_22,
    OfficeTwin_eqFunction_23,
    OfficeTwin_eqFunction_24,
    OfficeTwin_eqFunction_25,
    OfficeTwin_eqFunction_26,
    OfficeTwin_eqFunction_27,
    OfficeTwin_eqFunction_28,
    OfficeTwin_eqFunction_29,
    OfficeTwin_eqFunction_30,
    OfficeTwin_eqFunction_31
  };
  
  for (int id = 0; id < 15; id++) {
    eqFunctions[id](data, threadData);
  }
  data->simulationInfo->discreteCall = 0;
  
#if !defined(OMC_MINIMAL_RUNTIME)
  if (measure_time_flag) rt_accumulate(SIM_TIMER_DAE);
#endif
  return 0;
}


int OfficeTwin_functionLocalKnownVars(DATA *data, threadData_t *threadData)
{
  
  return 0;
}

/* forwarded equations */
extern void OfficeTwin_eqFunction_17(DATA* data, threadData_t *threadData);
extern void OfficeTwin_eqFunction_18(DATA* data, threadData_t *threadData);
extern void OfficeTwin_eqFunction_19(DATA* data, threadData_t *threadData);
extern void OfficeTwin_eqFunction_20(DATA* data, threadData_t *threadData);
extern void OfficeTwin_eqFunction_21(DATA* data, threadData_t *threadData);

static void functionODE_system0(DATA *data, threadData_t *threadData)
{
  static void (*const eqFunctions[5])(DATA*, threadData_t*) = {
    OfficeTwin_eqFunction_17,
    OfficeTwin_eqFunction_18,
    OfficeTwin_eqFunction_19,
    OfficeTwin_eqFunction_20,
    OfficeTwin_eqFunction_21
  };
  
  for (int id = 0; id < 5; id++) {
    eqFunctions[id](data, threadData);
  }
}

int OfficeTwin_functionODE(DATA *data, threadData_t *threadData)
{
#if !defined(OMC_MINIMAL_RUNTIME)
  if (measure_time_flag) rt_tick(SIM_TIMER_FUNCTION_ODE);
#endif

  
  data->simulationInfo->callStatistics.functionODE++;
  
  OfficeTwin_functionLocalKnownVars(data, threadData);
  functionODE_system0(data, threadData);

#if !defined(OMC_MINIMAL_RUNTIME)
  if (measure_time_flag) rt_accumulate(SIM_TIMER_FUNCTION_ODE);
#endif

  return 0;
}

/* forward the main in the simulation runtime */
extern int _main_SimulationRuntime(int argc, char **argv, DATA *data, threadData_t *threadData);
extern int _main_OptimizationRuntime(int argc, char **argv, DATA *data, threadData_t *threadData);

#include "OfficeTwin_12jac.h"
#include "OfficeTwin_13opt.h"

struct OpenModelicaGeneratedFunctionCallbacks OfficeTwin_callback = {
  NULL,    /* performSimulation */
  NULL,    /* performQSSSimulation */
  NULL,    /* updateContinuousSystem */
  OfficeTwin_callExternalObjectDestructors,    /* callExternalObjectDestructors */
  NULL,    /* initialNonLinearSystem */
  NULL,    /* initialLinearSystem */
  NULL,    /* initialMixedSystem */
  #if !defined(OMC_NO_STATESELECTION)
  OfficeTwin_initializeStateSets,
  #else
  NULL,
  #endif    /* initializeStateSets */
  OfficeTwin_initializeDAEmodeData,
  OfficeTwin_functionODE,
  OfficeTwin_functionAlgebraics,
  OfficeTwin_functionDAE,
  OfficeTwin_functionLocalKnownVars,
  OfficeTwin_input_function,
  OfficeTwin_input_function_init,
  OfficeTwin_input_function_updateStartValues,
  OfficeTwin_data_function,
  OfficeTwin_output_function,
  OfficeTwin_setc_function,
  OfficeTwin_setb_function,
  OfficeTwin_function_storeDelayed,
  OfficeTwin_function_storeSpatialDistribution,
  OfficeTwin_function_initSpatialDistribution,
  OfficeTwin_updateBoundVariableAttributes,
  OfficeTwin_functionInitialEquations,
  GLOBAL_EQUIDISTANT_HOMOTOPY,
  NULL,
  OfficeTwin_functionRemovedInitialEquations,
  OfficeTwin_updateBoundParameters,
  OfficeTwin_checkForAsserts,
  OfficeTwin_function_ZeroCrossingsEquations,
  OfficeTwin_function_ZeroCrossings,
  OfficeTwin_function_updateRelations,
  OfficeTwin_zeroCrossingDescription,
  OfficeTwin_relationDescription,
  OfficeTwin_function_initSample,
  OfficeTwin_INDEX_JAC_A,
  OfficeTwin_INDEX_JAC_B,
  OfficeTwin_INDEX_JAC_C,
  OfficeTwin_INDEX_JAC_D,
  OfficeTwin_INDEX_JAC_F,
  OfficeTwin_INDEX_JAC_H,
  OfficeTwin_initialAnalyticJacobianA,
  OfficeTwin_initialAnalyticJacobianB,
  OfficeTwin_initialAnalyticJacobianC,
  OfficeTwin_initialAnalyticJacobianD,
  OfficeTwin_initialAnalyticJacobianF,
  OfficeTwin_initialAnalyticJacobianH,
  OfficeTwin_functionJacA_column,
  OfficeTwin_functionJacB_column,
  OfficeTwin_functionJacC_column,
  OfficeTwin_functionJacD_column,
  OfficeTwin_functionJacF_column,
  OfficeTwin_functionJacH_column,
  OfficeTwin_linear_model_frame,
  OfficeTwin_linear_model_datarecovery_frame,
  OfficeTwin_mayer,
  OfficeTwin_lagrange,
  OfficeTwin_getInputVarIndicesInOptimization,
  OfficeTwin_pickUpBoundsForInputsInOptimization,
  OfficeTwin_setInputData,
  OfficeTwin_getTimeGrid,
  OfficeTwin_symbolicInlineSystem,
  OfficeTwin_function_initSynchronous,
  OfficeTwin_function_updateSynchronous,
  OfficeTwin_function_equationsSynchronous,
  OfficeTwin_inputNames,
  OfficeTwin_dataReconciliationInputNames,
  OfficeTwin_dataReconciliationUnmeasuredVariables,
  OfficeTwin_read_simulation_info,
  OfficeTwin_read_input_fmu,
  NULL,
  NULL,
  -1,
  NULL,
  NULL,
  -1

};

#define _OMC_LIT_RESOURCE_0_name_data "OfficeTwin"
#define _OMC_LIT_RESOURCE_0_dir_data "C:/dev/ruleless-digital-twins/SmartNode/Implementations/FMUs"
static const MMC_DEFSTRINGLIT(_OMC_LIT_RESOURCE_0_name,10,_OMC_LIT_RESOURCE_0_name_data);
static const MMC_DEFSTRINGLIT(_OMC_LIT_RESOURCE_0_dir,60,_OMC_LIT_RESOURCE_0_dir_data);

static const MMC_DEFSTRUCTLIT(_OMC_LIT_RESOURCES,2,MMC_ARRAY_TAG) {MMC_REFSTRINGLIT(_OMC_LIT_RESOURCE_0_name), MMC_REFSTRINGLIT(_OMC_LIT_RESOURCE_0_dir)}};
void OfficeTwin_setupDataStruc(DATA *data, threadData_t *threadData)
{
  assertStreamPrint(threadData,0!=data, "Error while initialize Data");
  threadData->localRoots[LOCAL_ROOT_SIMULATION_DATA] = data;
  data->callback = &OfficeTwin_callback;
  OpenModelica_updateUriMapping(threadData, MMC_REFSTRUCTLIT(_OMC_LIT_RESOURCES));
  data->modelData->modelName = "OfficeTwin";
  data->modelData->modelFilePrefix = "OfficeTwin";
  data->modelData->modelFileName = "OfficeTwin.mo";
  data->modelData->resultFileName = NULL;
  data->modelData->modelDir = "C:/dev/ruleless-digital-twins/SmartNode/Implementations/FMUs";
  data->modelData->modelGUID = "{cbc3f2fa-dd86-4ced-a7ab-eb1f86a76d70}";
  data->modelData->initXMLData = NULL;
  data->modelData->modelDataXml.infoXMLData = NULL;
  GC_asprintf(&data->modelData->modelDataXml.fileName, "%s/OfficeTwin_info.json", data->modelData->resourcesDir);
  data->modelData->runTestsuite = 0;
  data->modelData->nStatesArray = 2;
  data->modelData->nDiscreteReal = 0;
  data->modelData->nVariablesRealArray = 17;
  data->modelData->nVariablesIntegerArray = 3;
  data->modelData->nVariablesBooleanArray = 0;
  data->modelData->nVariablesStringArray = 0;
  data->modelData->nParametersRealArray = 16;
  data->modelData->nParametersIntegerArray = 0;
  data->modelData->nParametersBooleanArray = 0;
  data->modelData->nParametersStringArray = 0;
  data->modelData->nParametersReal = 16;
  data->modelData->nParametersInteger = 0;
  data->modelData->nParametersBoolean = 0;
  data->modelData->nParametersString = 0;
  data->modelData->nAliasRealArray = 1;
  data->modelData->nAliasIntegerArray = 0;
  data->modelData->nAliasBooleanArray = 0;
  data->modelData->nAliasStringArray = 0;
  data->modelData->nInputVars = 3;
  data->modelData->nOutputVars = 2;
  data->modelData->nZeroCrossings = 3;
  data->modelData->nSamples = 0;
  data->modelData->nRelations = 3;
  data->modelData->nMathEvents = 0;
  data->modelData->nExtObjs = 0;
  data->modelData->modelDataXml.modelInfoXmlLength = 0;
  data->modelData->modelDataXml.nFunctions = 0;
  data->modelData->modelDataXml.nProfileBlocks = 0;
  data->modelData->modelDataXml.nEquations = 36;
  data->modelData->nMixedSystems = 0;
  data->modelData->nLinearSystems = 0;
  data->modelData->nNonLinearSystems = 0;
  data->modelData->nStateSets = 0;
  data->modelData->nJacobians = 6;
  data->modelData->nOptimizeConstraints = 0;
  data->modelData->nOptimizeFinalConstraints = 0;
  data->modelData->nDelayExpressions = 0;
  data->modelData->nBaseClocks = 0;
  data->modelData->nSpatialDistributions = 0;
  data->modelData->nSensitivityVars = 0;
  data->modelData->nSensitivityParamVars = 0;
  data->modelData->nSetcVars = 0;
  data->modelData->ndataReconVars = 0;
  data->modelData->nSetbVars = 0;
  data->modelData->nRelatedBoundaryConditions = 0;
  data->modelData->linearizationDumpLanguage = OMC_LINEARIZE_DUMP_LANGUAGE_MODELICA;
}

static int rml_execution_failed()
{
  fflush(NULL);
  fprintf(stderr, "Execution failed!\n");
  fflush(NULL);
  return 1;
}

