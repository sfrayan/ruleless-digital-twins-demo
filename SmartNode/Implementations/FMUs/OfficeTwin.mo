model OfficeTwin
  "Physical twin of a Nordic office room (temperature + humidity dynamics).
   Designed for use with the ruleless-digital-twins MAPE-K loop.

   Inputs  (set by SmartNode after initialization):
     HeaterActuator        - electric panel heater  (0=off, 1=on)
     FloorHeatingActuator  - hydronic floor heating (0=off, 1=on)
     DehumidifierActuator  - dehumidifier           (0=off, 1=on)

   Outputs (matched to PropertyCache via URI EndsWith):
     Temperature  -> ha:OfficeTemperature  [degC]
     Humidity     -> ha:OfficeHumidity     [% RH]

   Room: 4 m x 5 m x 2.7 m = 54 m3, Nordic winter, T_out = -5 degC.
  "

  // ── Actuator inputs (FMI Integer inputs) ────────────────────────
  input Integer HeaterActuator(start = 0)
    "Electric panel heater state (0 = off, 1 = on)";
  input Integer FloorHeatingActuator(start = 0)
    "Floor heating state (0 = off, 1 = on)";
  input Integer DehumidifierActuator(start = 0)
    "Dehumidifier state (0 = off, 1 = on)";

  // ── Room geometry ────────────────────────────────────────────────
  parameter Real V    = 54.0   "Room volume [m3] — 4 m x 5 m x 2.7 m";
  parameter Real rho  = 1.2    "Air density [kg/m3] at indoor conditions";
  parameter Real cp   = 1005.0 "Specific heat of air [J/(kg.K)]";

  // ── Thermal parameters ───────────────────────────────────────────
  // UA = envelope (walls 0.3 W/m2K, windows 1.5 W/m2K, ceiling 0.15, floor 0.2)
  // + balanced mechanical ventilation 0.5 ACH.
  // Calibrated so floor heating alone (1500 W) reaches ~20 degC SS from -5 degC.
  parameter Real UA     = 50.0   "Total thermal conductance [W/K] (envelope + ventilation)";
  parameter Real T_out  = -5.0   "Outdoor temperature [degC], Nordic winter";
  parameter Real P_htr  = 2000.0 "Electric heater rated power [W]";
  parameter Real P_flr  = 1500.0 "Floor heating rated power [W]  (75 W/m2 x 20 m2)";
  parameter Real Q_int  = 350.0  "Internal + solar gains [W] (1 person 70W + PC 150W + lighting 30W + solar 100W)";

  // ── Moisture / humidity parameters ──────────────────────────────
  parameter Real n_ach    = 0.5    "Air changes per hour [1/h] — balanced ventilation + infiltration";
  parameter Real m_per    = 0.0139 "Moisture generation by 1 person [g/s] = 50 g/h, sedentary";
  parameter Real m_deh    = 0.05   "Dehumidifier moisture removal capacity [g/s] = 180 g/h";
  parameter Real RH_out   = 80.0   "Outdoor relative humidity [%]";

  // ── Derived constants ────────────────────────────────────────────
  parameter Real m_air  = rho * V           "Total air mass in room [kg]";
  parameter Real m_vent = n_ach / 3600.0 * m_air "Ventilation mass flow rate [kg/s]";

  // Outdoor saturation pressure (Magnus formula) and absolute humidity
  parameter Real Psat_out = 610.78 * exp(17.27 * T_out / (T_out + 237.3))
    "Outdoor saturation vapour pressure [Pa]";
  parameter Real W_out = 622.0 * (RH_out / 100.0) * Psat_out
                         / (101325.0 - (RH_out / 100.0) * Psat_out)
    "Outdoor absolute humidity [g/kg_air]";

  // ── State variables ──────────────────────────────────────────────
  // Initial values match the default HA sensor readings in the instance model.
  Real T(start = 16.7, fixed = true)
    "Room air temperature [degC]";
  Real W(start = 0.962, fixed = true)
    "Room absolute humidity [g/kg_air]  (= 8.1 % RH at 16.7 degC)";

  // ── Algebraic (instantaneous) quantities ─────────────────────────
  Real Psat_in
    "Indoor saturation vapour pressure [Pa] — function of T";
  Real W_sat
    "Indoor saturation specific humidity [g/kg_air]";
  Real Q_heat
    "Total heating power actually delivered [W]";
  Real Q_loss
    "Heat loss to outside through envelope and ventilation [W]";

  // ── Outputs ──────────────────────────────────────────────────────
  // Names chosen so that their suffix matches the ha: PropertyCache URIs
  // via the EndsWith check in MapekPlan.AssignPropertyCacheCopyValues.
  output Real Temperature
    "Predicted room temperature [degC]  — matches ha:OfficeTemperature";
  output Real Humidity
    "Predicted relative humidity [% RH] — matches ha:OfficeHumidity";

equation
  // Saturation pressure via Magnus formula (valid -40 to +60 degC)
  Psat_in = 610.78 * exp(17.27 * T / (T + 237.3));

  // Saturation specific humidity [g/kg_air]
  W_sat = 622.0 * Psat_in / (101325.0 - Psat_in);

  // Delivered heating power [W]
  Q_heat = P_htr * HeaterActuator + P_flr * FloorHeatingActuator;

  // Fabric + ventilation heat loss [W]
  Q_loss = UA * (T - T_out);

  // Thermal ODE: energy balance on room air [degC/s]
  der(T) = (Q_heat + Q_int - Q_loss) / (m_air * cp);

  // Moisture ODE: mass balance on room air [g/kg_air / s]
  // Sources: occupant, ventilation intake
  // Sinks:   ventilation exhaust, dehumidifier
  der(W) = (m_per + m_vent * (W_out - W) - m_deh * DehumidifierActuator) / m_air;

  // Output assignments
  Temperature = T;
  Humidity = if W_sat <= 0.0 then 0.0
             else if W / W_sat >= 1.0 then 100.0
             else if W / W_sat <= 0.0 then 0.0
             else 100.0 * W / W_sat;

  annotation(
    experiment(StopTime = 3600, Tolerance = 1e-6),
    Documentation(info = "<html>
      <p>Nordic office twin for ruleless-digital-twins MAPE-K loop.</p>
      <p>See model header for full parameter documentation.</p>
    </html>")
  );
end OfficeTwin;
