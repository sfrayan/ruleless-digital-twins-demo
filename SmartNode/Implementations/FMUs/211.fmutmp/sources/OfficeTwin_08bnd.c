/* update bound parameters and variable attributes (start, nominal, min, max) */
#include "OfficeTwin_model.h"
#if defined(__cplusplus)
extern "C" {
#endif

OMC_DISABLE_OPT
int OfficeTwin_updateBoundVariableAttributes(DATA *data, threadData_t *threadData)
{
  /* min ******************************************************** */
  infoStreamPrint(OMC_LOG_INIT, 1, "updating min-values");
  messageClose(OMC_LOG_INIT);
  
  /* max ******************************************************** */
  infoStreamPrint(OMC_LOG_INIT, 1, "updating max-values");
  messageClose(OMC_LOG_INIT);
  
  /* nominal **************************************************** */
  infoStreamPrint(OMC_LOG_INIT, 1, "updating nominal-values");
  messageClose(OMC_LOG_INIT);
  
  /* start ****************************************************** */
  infoStreamPrint(OMC_LOG_INIT, 1, "updating primary start-values");
  messageClose(OMC_LOG_INIT);
  
  return 0;
}

void OfficeTwin_updateBoundParameters_0(DATA *data, threadData_t *threadData);

/*
equation index: 32
type: SIMPLE_ASSIGN
Psat_out = 610.78 * exp(17.27 * T_out / (T_out + 237.3))
*/
OMC_DISABLE_OPT
static void OfficeTwin_eqFunction_32(DATA *data, threadData_t *threadData)
{
  const int equationIndexes[2] = {1,32};
  (data->simulationInfo->realParameter[data->simulationInfo->realParamsIndex[2]] /* Psat_out PARAM */) = (610.78) * (exp(DIVISION_SIM((17.27) * ((data->simulationInfo->realParameter[data->simulationInfo->realParamsIndex[5]] /* T_out PARAM */)),(data->simulationInfo->realParameter[data->simulationInfo->realParamsIndex[5]] /* T_out PARAM */) + 237.3,"T_out + 237.3",equationIndexes)));
  threadData->lastEquationSolved = 32;
}

/*
equation index: 33
type: SIMPLE_ASSIGN
W_out = 6.22 * RH_out * Psat_out / (101325.0 + (-0.01) * RH_out * Psat_out)
*/
OMC_DISABLE_OPT
static void OfficeTwin_eqFunction_33(DATA *data, threadData_t *threadData)
{
  const int equationIndexes[2] = {1,33};
  (data->simulationInfo->realParameter[data->simulationInfo->realParamsIndex[8]] /* W_out PARAM */) = (6.22) * (((data->simulationInfo->realParameter[data->simulationInfo->realParamsIndex[4]] /* RH_out PARAM */)) * (DIVISION_SIM((data->simulationInfo->realParameter[data->simulationInfo->realParamsIndex[2]] /* Psat_out PARAM */),101325.0 + (-0.01) * (((data->simulationInfo->realParameter[data->simulationInfo->realParamsIndex[4]] /* RH_out PARAM */)) * ((data->simulationInfo->realParameter[data->simulationInfo->realParamsIndex[2]] /* Psat_out PARAM */))),"101325.0 + (-0.01) * RH_out * Psat_out",equationIndexes)));
  threadData->lastEquationSolved = 33;
}

/*
equation index: 34
type: SIMPLE_ASSIGN
m_air = rho * V
*/
OMC_DISABLE_OPT
static void OfficeTwin_eqFunction_34(DATA *data, threadData_t *threadData)
{
  const int equationIndexes[2] = {1,34};
  (data->simulationInfo->realParameter[data->simulationInfo->realParamsIndex[10]] /* m_air PARAM */) = ((data->simulationInfo->realParameter[data->simulationInfo->realParamsIndex[15]] /* rho PARAM */)) * ((data->simulationInfo->realParameter[data->simulationInfo->realParamsIndex[7]] /* V PARAM */));
  threadData->lastEquationSolved = 34;
}

/*
equation index: 35
type: SIMPLE_ASSIGN
m_vent = 2.777777777777778e-4 * n_ach * m_air
*/
OMC_DISABLE_OPT
static void OfficeTwin_eqFunction_35(DATA *data, threadData_t *threadData)
{
  const int equationIndexes[2] = {1,35};
  (data->simulationInfo->realParameter[data->simulationInfo->realParamsIndex[13]] /* m_vent PARAM */) = (2.777777777777778e-4) * (((data->simulationInfo->realParameter[data->simulationInfo->realParamsIndex[14]] /* n_ach PARAM */)) * ((data->simulationInfo->realParameter[data->simulationInfo->realParamsIndex[10]] /* m_air PARAM */)));
  threadData->lastEquationSolved = 35;
}
OMC_DISABLE_OPT
void OfficeTwin_updateBoundParameters_0(DATA *data, threadData_t *threadData)
{
  static void (*const eqFunctions[4])(DATA*, threadData_t*) = {
    OfficeTwin_eqFunction_32,
    OfficeTwin_eqFunction_33,
    OfficeTwin_eqFunction_34,
    OfficeTwin_eqFunction_35
  };
  
  for (int id = 0; id < 4; id++) {
    eqFunctions[id](data, threadData);
  }
}
OMC_DISABLE_OPT
int OfficeTwin_updateBoundParameters(DATA *data, threadData_t *threadData)
{
  OfficeTwin_updateBoundParameters_0(data, threadData);
  return 0;
}

#if defined(__cplusplus)
}
#endif
