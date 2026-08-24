fusion-reactor-controller-ui-format-power = { POWERWATTS($power) }
fusion-reactor-controller-ui-format-percent = { TOSTRING($value, "P1") }
fusion-reactor-controller-ui-format-temperature = { TOSTRING($value, "F1") } k
fusion-reactor-controller-ui-format-pressure = { TOSTRING($value, "F1") } pa
fusion-reactor-controller-ui-format-seconds = { TOSTRING($value, "F1") }

fusion-reactor-controller-ui-unit-mol = mol

fusion-reactor-controller-ui-tab-contents = Contents
fusion-reactor-controller-ui-tab-injection = Injection
fusion-reactor-controller-ui-tab-powercontrol = Power Control
fusion-reactor-controller-ui-tab-masercontrol = MASER Control

fusion-reactor-controller-ui-sort = Sort
fusion-reactor-controller-ui-set = Set

fusion-reactor-controller-ui-edit-entries = Edit Entries
fusion-reactor-controller-ui-add = Add
fusion-reactor-controller-ui-remove = Remove

fusion-reactor-controller-ui-setrate = Set Rate
fusion-reactor-controller-ui-setlevel = Set Level
fusion-reactor-controller-ui-fill = Fill
fusion-reactor-controller-ui-drain = Drain

fusion-reactor-controller-ui-disabled = Disabled
fusion-reactor-controller-ui-watts = Watts
fusion-reactor-controller-ui-temperature = Temperature

fusion-reactor-controller-ui-req-pressure = Requested Magnetic Pressure

fusion-reactor-controller-ui-stat-temp = Plasma Temperature
fusion-reactor-controller-ui-stat-pressure = Magnetic Pressure
fusion-reactor-controller-ui-stat-expansion = Expansion
fusion-reactor-controller-ui-stat-stability = Stability
fusion-reactor-controller-ui-stat-integrity = Integrity
fusion-reactor-controller-ui-stat-magtemp = Magnet Temperature
fusion-reactor-controller-ui-stat-extraction = Power Extraction
fusion-reactor-controller-ui-stat-export = Power Export
fusion-reactor-controller-ui-stat-no-data = N/A

fusion-reactor-controller-ui-eject = Eject

# [8] has an invisible character, U+200B, because it does not accept empty strings
fusion-reactor-controller-ui-fmt-prefix = { TOSTRING($divided, "F1") } { $places ->
    [0] y
    [1] z
    [2] a
    [3] f
    [4] p
    [5] n
    [6] u
    [7] m
    [8] ​ 
    [9] k
    [10] M
    [11] G
    *[12] ???
}

fusion-reactor-controller-announcement-sender = Fusion Reactor
fusion-reactor-controller-announcement-stage-safe = The fusion reactor's integrity has recovered to acceptable levels. A containment breach has been averted.
fusion-reactor-controller-announcement-stage-2 = A fusion reactor on board the station has suffered extreme integrity loss. A containment breach may be imminent.
fusion-reactor-controller-announcement-stage-3 = A fusion reactor on board the station has suffered catastorphic integrity loss. The core eject button has been activated. Estimated { TOSTRING($value, "F1") } seconds to containment failure.
fusion-reactor-controller-announcement-stage-4 = A fusion reactor on board the station has suffered a complete containment failure. Emergency containment fields have been deployed but have proved ineffective. The reactor will detonate in { TOSTRING($value, "F1") } seconds. Evacuate the area immediately.

fusion-reactor-controller-radio-integrity-rising = Integrity recovering: { TOSTRING($value, "P1") }.
fusion-reactor-controller-radio-integrity-falling = WARNING: integrity loss detected. Current integrity: { TOSTRING($value, "P1") }.
fusion-reactor-controller-radio-integrity-restored = Integrity fully recovered.
fusion-reactor-controller-radio-stage-2 = Attempting emergency coolant dump at 5% integrity.