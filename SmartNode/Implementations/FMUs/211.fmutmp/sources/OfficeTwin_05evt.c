/* Events: Sample, Zero Crossings, Relations, Discrete Changes */
#include "OfficeTwin_model.h"
#if defined(__cplusplus)
extern "C" {
#endif

/* Initializes the raw time events of the simulation using the now
   calcualted parameters. */
void OfficeTwin_function_initSample(DATA *data, threadData_t *threadData)
{
  long i=0;
}

const char *OfficeTwin_zeroCrossingDescription(int i, int **out_EquationIndexes)
{
  static const char *res[] = {"W_sat <= 0.0",
  "W / W_sat >= 1.0",
  "W / W_sat <= 0.0"};
  static const int occurEqs0[] = {1,26};
  static const int occurEqs1[] = {1,26};
  static const int occurEqs2[] = {1,26};
  static const int *occurEqs[] = {occurEqs0,occurEqs1,occurEqs2};
  *out_EquationIndexes = (int*) occurEqs[i];
  return res[i];
}

/* forwarded equations */
extern void OfficeTwin_eqFunction_17(DATA* data, threadData_t *threadData);
extern void OfficeTwin_eqFunction_18(DATA* data, threadData_t *threadData);
extern void OfficeTwin_eqFunction_19(DATA* data, threadData_t *threadData);
extern void OfficeTwin_eqFunction_20(DATA* data, threadData_t *threadData);
extern void OfficeTwin_eqFunction_21(DATA* data, threadData_t *threadData);
extern void OfficeTwin_eqFunction_23(DATA* data, threadData_t *threadData);
extern void OfficeTwin_eqFunction_24(DATA* data, threadData_t *threadData);
extern void OfficeTwin_eqFunction_25(DATA* data, threadData_t *threadData);

int OfficeTwin_function_ZeroCrossingsEquations(DATA *data, threadData_t *threadData)
{
  data->simulationInfo->callStatistics.functionZeroCrossingsEquations++;

  static void (*const eqFunctions[8])(DATA*, threadData_t*) = {
    OfficeTwin_eqFunction_17,
    OfficeTwin_eqFunction_18,
    OfficeTwin_eqFunction_19,
    OfficeTwin_eqFunction_20,
    OfficeTwin_eqFunction_21,
    OfficeTwin_eqFunction_23,
    OfficeTwin_eqFunction_24,
    OfficeTwin_eqFunction_25
  };
  
  for (int id = 0; id < 8; id++) {
    eqFunctions[id](data, threadData);
  }
  
  return 0;
}

int OfficeTwin_function_ZeroCrossings(DATA *data, threadData_t *threadData, double *gout)
{
  const int *equationIndexes = NULL;

  modelica_boolean tmp0;
  modelica_real tmp1;
  modelica_real tmp2;
  modelica_boolean tmp3;
  modelica_real tmp4;
  modelica_real tmp5;
  modelica_boolean tmp6;
  modelica_real tmp7;
  modelica_real tmp8;
  modelica_integer current_index = 0;
  modelica_integer start_index;
  
#if !defined(OMC_MINIMAL_RUNTIME)
  if (measure_time_flag) rt_tick(SIM_TIMER_ZC);
#endif
  data->simulationInfo->callStatistics.functionZeroCrossings++;

  start_index = current_index;
  tmp1 = 1.0;
  tmp2 = 0.0;
  tmp0 = LessEqZC((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[16]] /* W_sat DUMMY_STATE */), 0.0, tmp1, tmp2, data->simulationInfo->storedRelations[0]);
  gout[start_index] = (tmp0) ? 1 : -1;
  current_index++;

  start_index = current_index;
  tmp4 = 1.0;
  tmp5 = 1.0;
  tmp3 = GreaterEqZC(DIVISION((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[1]] /* W STATE(1) */),(data->localData[0]->realVars[data->simulationInfo->realVarsIndex[16]] /* W_sat DUMMY_STATE */),"W_sat"), 1.0, tmp4, tmp5, data->simulationInfo->storedRelations[1]);
  gout[start_index] = (tmp3) ? 1 : -1;
  current_index++;

  start_index = current_index;
  tmp7 = 1.0;
  tmp8 = 0.0;
  tmp6 = LessEqZC(DIVISION((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[1]] /* W STATE(1) */),(data->localData[0]->realVars[data->simulationInfo->realVarsIndex[16]] /* W_sat DUMMY_STATE */),"W_sat"), 0.0, tmp7, tmp8, data->simulationInfo->storedRelations[2]);
  gout[start_index] = (tmp6) ? 1 : -1;
  current_index++;

#if !defined(OMC_MINIMAL_RUNTIME)
  if (measure_time_flag) rt_accumulate(SIM_TIMER_ZC);
#endif

  return 0;
}

const char *OfficeTwin_relationDescription(int i)
{
  const char *res[] = {"W_sat <= 0.0",
  "W / W_sat >= 1.0",
  "W / W_sat <= 0.0"};
  return res[i];
}

int OfficeTwin_function_updateRelations(DATA *data, threadData_t *threadData, int evalforZeroCross)
{
  const int *equationIndexes = NULL;

  modelica_boolean tmp9;
  modelica_real tmp10;
  modelica_real tmp11;
  modelica_boolean tmp12;
  modelica_real tmp13;
  modelica_real tmp14;
  modelica_boolean tmp15;
  modelica_real tmp16;
  modelica_real tmp17;
  modelica_integer current_index = 0;
  modelica_integer start_index;
  
  if(evalforZeroCross) {
    start_index = current_index;
    tmp10 = 1.0;
    tmp11 = 0.0;
    tmp9 = LessEqZC((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[16]] /* W_sat DUMMY_STATE */), 0.0, tmp10, tmp11, data->simulationInfo->storedRelations[0]);
    data->simulationInfo->relations[start_index] = tmp9;
    current_index++;

    start_index = current_index;
    tmp13 = 1.0;
    tmp14 = 1.0;
    tmp12 = GreaterEqZC(DIVISION((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[1]] /* W STATE(1) */),(data->localData[0]->realVars[data->simulationInfo->realVarsIndex[16]] /* W_sat DUMMY_STATE */),"W_sat"), 1.0, tmp13, tmp14, data->simulationInfo->storedRelations[1]);
    data->simulationInfo->relations[start_index] = tmp12;
    current_index++;

    start_index = current_index;
    tmp16 = 1.0;
    tmp17 = 0.0;
    tmp15 = LessEqZC(DIVISION((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[1]] /* W STATE(1) */),(data->localData[0]->realVars[data->simulationInfo->realVarsIndex[16]] /* W_sat DUMMY_STATE */),"W_sat"), 0.0, tmp16, tmp17, data->simulationInfo->storedRelations[2]);
    data->simulationInfo->relations[start_index] = tmp15;
    current_index++;
  } else {
    start_index = current_index;
    data->simulationInfo->relations[start_index] = ((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[16]] /* W_sat DUMMY_STATE */) <= 0.0);
    current_index++;

    start_index = current_index;
    data->simulationInfo->relations[start_index] = (DIVISION((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[1]] /* W STATE(1) */),(data->localData[0]->realVars[data->simulationInfo->realVarsIndex[16]] /* W_sat DUMMY_STATE */),"W_sat") >= 1.0);
    current_index++;

    start_index = current_index;
    data->simulationInfo->relations[start_index] = (DIVISION((data->localData[0]->realVars[data->simulationInfo->realVarsIndex[1]] /* W STATE(1) */),(data->localData[0]->realVars[data->simulationInfo->realVarsIndex[16]] /* W_sat DUMMY_STATE */),"W_sat") <= 0.0);
    current_index++;
  }
  
  return 0;
}

#if defined(__cplusplus)
}
#endif
