/* Initialization */
#include "OfficeTwin_model.h"
#include "OfficeTwin_11mix.h"
#include "OfficeTwin_12jac.h"
#if defined(__cplusplus)
extern "C" {
#endif

void OfficeTwin_functionInitialEquations_0(DATA *data, threadData_t *threadData);
extern void OfficeTwin_eqFunction_19(DATA *data, threadData_t *threadData);


/*
equation index: 2
type: SIMPLE_ASSIGN
W = $START.W
*/
void OfficeTwin_eqFunction_2(DATA *data, threadData_t *threadData)
{
  const int equationIndexes[2] = {1,2};
  (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[1]] /* W STATE(1) */) = ((modelica_real *)((data->modelData->realVarsData[1] /* W STATE(1) */).attribute .start.data))[0];
  threadData->lastEquationSolved = 2;
}
extern void OfficeTwin_eqFunction_17(DATA *data, threadData_t *threadData);


/*
equation index: 4
type: SIMPLE_ASSIGN
$outputAlias_Temperature = $START.$outputAlias_Temperature
*/
void OfficeTwin_eqFunction_4(DATA *data, threadData_t *threadData)
{
  const int equationIndexes[2] = {1,4};
  (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[0]] /* $outputAlias_Temperature STATE(1,$Temperature_der) */) = ((modelica_real *)((data->modelData->realVarsData[0] /* $outputAlias_Temperature STATE(1,$Temperature_der) */).attribute .start.data))[0];
  threadData->lastEquationSolved = 4;
}

/*
equation index: 5
type: SIMPLE_ASSIGN
Psat_in = 610.78 * exp(17.27 * $outputAlias_Temperature / ($outputAlias_Temperature + 237.3))
*/
void OfficeTwin_eqFunction_5(DATA *data, threadData_t *threadData)
{
  const int equationIndexes[2] = {1,5};
  (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[12]] /* Psat_in DUMMY_STATE */) = (610.78) * (exp(DIVISION_SIM((17.27) * ((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[0]] /* $outputAlias_Temperature STATE(1,$Temperature_der) */)),(data->localData[0]->realVars[data->simulationInfo->realVarsIndex[0]] /* $outputAlias_Temperature STATE(1,$Temperature_der) */) + 237.3,"$outputAlias_Temperature + 237.3",equationIndexes)));
  threadData->lastEquationSolved = 5;
}
extern void OfficeTwin_eqFunction_25(DATA *data, threadData_t *threadData);


/*
equation index: 7
type: SIMPLE_ASSIGN
$outputAlias_Humidity = if W_sat <= 0.0 then 0.0 else if W / W_sat >= 1.0 then 100.0 else if W / W_sat <= 0.0 then 0.0 else 100.0 * W / W_sat
*/
void OfficeTwin_eqFunction_7(DATA *data, threadData_t *threadData)
{
  const int equationIndexes[2] = {1,7};
  modelica_boolean tmp0;
  modelica_boolean tmp1;
  modelica_boolean tmp2;
  modelica_boolean tmp3;
  modelica_real tmp4;
  modelica_boolean tmp5;
  modelica_real tmp6;
  tmp0 = LessEq((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[16]] /* W_sat DUMMY_STATE */),0.0);
  tmp5 = (modelica_boolean)tmp0;
  if(tmp5)
  {
    tmp6 = 0.0;
  }
  else
  {
    tmp1 = GreaterEq(DIVISION_SIM((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[1]] /* W STATE(1) */),(data->localData[0]->realVars[data->simulationInfo->realVarsIndex[16]] /* W_sat DUMMY_STATE */),"W_sat",equationIndexes),1.0);
    tmp3 = (modelica_boolean)tmp1;
    if(tmp3)
    {
      tmp4 = 100.0;
    }
    else
    {
      tmp2 = LessEq(DIVISION_SIM((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[1]] /* W STATE(1) */),(data->localData[0]->realVars[data->simulationInfo->realVarsIndex[16]] /* W_sat DUMMY_STATE */),"W_sat",equationIndexes),0.0);
      tmp4 = (tmp2?0.0:DIVISION_SIM((100.0) * ((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[1]] /* W STATE(1) */)),(data->localData[0]->realVars[data->simulationInfo->realVarsIndex[16]] /* W_sat DUMMY_STATE */),"W_sat",equationIndexes));
    }
    tmp6 = tmp4;
  }
  (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[10]] /* $outputAlias_Humidity DUMMY_STATE */) = tmp6;
  threadData->lastEquationSolved = 7;
}

/*
equation index: 8
type: SIMPLE_ASSIGN
Humidity = $outputAlias_Humidity
*/
void OfficeTwin_eqFunction_8(DATA *data, threadData_t *threadData)
{
  const int equationIndexes[2] = {1,8};
  (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[11]] /* Humidity variable */) = (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[10]] /* $outputAlias_Humidity DUMMY_STATE */);
  threadData->lastEquationSolved = 8;
}
extern void OfficeTwin_eqFunction_22(DATA *data, threadData_t *threadData);

extern void OfficeTwin_eqFunction_18(DATA *data, threadData_t *threadData);

extern void OfficeTwin_eqFunction_20(DATA *data, threadData_t *threadData);

extern void OfficeTwin_eqFunction_21(DATA *data, threadData_t *threadData);


/*
equation index: 13
type: SIMPLE_ASSIGN
$DER.Psat_in = 10548.1706 * exp(17.27 * $outputAlias_Temperature / ($outputAlias_Temperature + 237.3)) * 237.3 * $Temperature_der / (237.3 + $outputAlias_Temperature) ^ 2.0
*/
void OfficeTwin_eqFunction_13(DATA *data, threadData_t *threadData)
{
  const int equationIndexes[2] = {1,13};
  modelica_real tmp7;
  tmp7 = 237.3 + (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[0]] /* $outputAlias_Temperature STATE(1,$Temperature_der) */);
  (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[5]] /* der(Psat_in) DUMMY_DER */) = (10548.1706) * ((exp(DIVISION_SIM((17.27) * ((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[0]] /* $outputAlias_Temperature STATE(1,$Temperature_der) */)),(data->localData[0]->realVars[data->simulationInfo->realVarsIndex[0]] /* $outputAlias_Temperature STATE(1,$Temperature_der) */) + 237.3,"$outputAlias_Temperature + 237.3",equationIndexes))) * ((237.3) * (DIVISION_SIM((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[8]] /* $Temperature_der variable */),(tmp7 * tmp7),"(237.3 + $outputAlias_Temperature) ^ 2.0",equationIndexes))));
  threadData->lastEquationSolved = 13;
}
extern void OfficeTwin_eqFunction_29(DATA *data, threadData_t *threadData);


/*
equation index: 15
type: SIMPLE_ASSIGN
$Humidity_der = if W_sat <= 0.0 then 0.0 else if W / W_sat >= 1.0 then 0.0 else if W / W_sat <= 0.0 then 0.0 else (100.0 * $DER.W * W_sat - 100.0 * W * $DER.W_sat) / W_sat ^ 2.0
*/
void OfficeTwin_eqFunction_15(DATA *data, threadData_t *threadData)
{
  const int equationIndexes[2] = {1,15};
  modelica_boolean tmp8;
  modelica_boolean tmp9;
  modelica_boolean tmp10;
  modelica_real tmp11;
  modelica_boolean tmp12;
  modelica_real tmp13;
  modelica_boolean tmp14;
  modelica_real tmp15;
  modelica_boolean tmp16;
  modelica_real tmp17;
  tmp8 = LessEq((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[16]] /* W_sat DUMMY_STATE */),0.0);
  tmp16 = (modelica_boolean)tmp8;
  if(tmp16)
  {
    tmp17 = 0.0;
  }
  else
  {
    tmp9 = GreaterEq(DIVISION_SIM((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[1]] /* W STATE(1) */),(data->localData[0]->realVars[data->simulationInfo->realVarsIndex[16]] /* W_sat DUMMY_STATE */),"W_sat",equationIndexes),1.0);
    tmp14 = (modelica_boolean)tmp9;
    if(tmp14)
    {
      tmp15 = 0.0;
    }
    else
    {
      tmp10 = LessEq(DIVISION_SIM((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[1]] /* W STATE(1) */),(data->localData[0]->realVars[data->simulationInfo->realVarsIndex[16]] /* W_sat DUMMY_STATE */),"W_sat",equationIndexes),0.0);
      tmp12 = (modelica_boolean)tmp10;
      if(tmp12)
      {
        tmp13 = 0.0;
      }
      else
      {
        tmp11 = (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[16]] /* W_sat DUMMY_STATE */);
        tmp13 = DIVISION_SIM(((100.0) * ((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[3]] /* der(W) STATE_DER */))) * ((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[16]] /* W_sat DUMMY_STATE */)) - (((100.0) * ((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[1]] /* W STATE(1) */))) * ((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[6]] /* der(W_sat) DUMMY_DER */))),(tmp11 * tmp11),"W_sat ^ 2.0",equationIndexes);
      }
      tmp15 = tmp13;
    }
    tmp17 = tmp15;
  }
  (data->localData[0]->realVars[data->simulationInfo->realVarsIndex[7]] /* $Humidity_der variable */) = tmp17;
  threadData->lastEquationSolved = 15;
}
extern void OfficeTwin_eqFunction_31(DATA *data, threadData_t *threadData);

OMC_DISABLE_OPT
void OfficeTwin_functionInitialEquations_0(DATA *data, threadData_t *threadData)
{
  static void (*const eqFunctions[16])(DATA*, threadData_t*) = {
    OfficeTwin_eqFunction_19,
    OfficeTwin_eqFunction_2,
    OfficeTwin_eqFunction_17,
    OfficeTwin_eqFunction_4,
    OfficeTwin_eqFunction_5,
    OfficeTwin_eqFunction_25,
    OfficeTwin_eqFunction_7,
    OfficeTwin_eqFunction_8,
    OfficeTwin_eqFunction_22,
    OfficeTwin_eqFunction_18,
    OfficeTwin_eqFunction_20,
    OfficeTwin_eqFunction_21,
    OfficeTwin_eqFunction_13,
    OfficeTwin_eqFunction_29,
    OfficeTwin_eqFunction_15,
    OfficeTwin_eqFunction_31
  };
  
  for (int id = 0; id < 16; id++) {
    eqFunctions[id](data, threadData);
  }
}

int OfficeTwin_functionInitialEquations(DATA *data, threadData_t *threadData)
{
  data->simulationInfo->discreteCall = 1;
  OfficeTwin_functionInitialEquations_0(data, threadData);
  data->simulationInfo->discreteCall = 0;
  
  return 0;
}

/* No OfficeTwin_functionInitialEquations_lambda0 function */

int OfficeTwin_functionRemovedInitialEquations(DATA *data, threadData_t *threadData)
{
  const int *equationIndexes = NULL;
  double res = 0.0;

  
  return 0;
}


#if defined(__cplusplus)
}
#endif
