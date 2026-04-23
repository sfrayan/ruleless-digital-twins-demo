/* Algebraic */
#include "OfficeTwin_model.h"

#ifdef __cplusplus
extern "C" {
#endif

/* forwarded equations */
extern void OfficeTwin_eqFunction_22(DATA* data, threadData_t *threadData);
extern void OfficeTwin_eqFunction_23(DATA* data, threadData_t *threadData);
extern void OfficeTwin_eqFunction_24(DATA* data, threadData_t *threadData);
extern void OfficeTwin_eqFunction_25(DATA* data, threadData_t *threadData);
extern void OfficeTwin_eqFunction_26(DATA* data, threadData_t *threadData);
extern void OfficeTwin_eqFunction_27(DATA* data, threadData_t *threadData);
extern void OfficeTwin_eqFunction_28(DATA* data, threadData_t *threadData);
extern void OfficeTwin_eqFunction_29(DATA* data, threadData_t *threadData);
extern void OfficeTwin_eqFunction_30(DATA* data, threadData_t *threadData);
extern void OfficeTwin_eqFunction_31(DATA* data, threadData_t *threadData);

static void functionAlg_system0(DATA *data, threadData_t *threadData)
{
  static void (*const eqFunctions[10])(DATA*, threadData_t*) = {
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
  
  for (int id = 0; id < 10; id++) {
    eqFunctions[id](data, threadData);
  }
}
/* for continuous time variables */
int OfficeTwin_functionAlgebraics(DATA *data, threadData_t *threadData)
{

#if !defined(OMC_MINIMAL_RUNTIME)
  if (measure_time_flag) rt_tick(SIM_TIMER_ALGEBRAICS);
#endif
  data->simulationInfo->callStatistics.functionAlgebraics++;

  OfficeTwin_function_savePreSynchronous(data, threadData);
  
  functionAlg_system0(data, threadData);

#if !defined(OMC_MINIMAL_RUNTIME)
  if (measure_time_flag) rt_accumulate(SIM_TIMER_ALGEBRAICS);
#endif

  return 0;
}

#ifdef __cplusplus
}
#endif
