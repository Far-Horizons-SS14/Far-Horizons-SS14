fusion-reactor-controller-ui-format-power = { POWERWATTS($power) }
fusion-reactor-controller-ui-format-percent = { TOSTRING($value, "P1") }
fusion-reactor-controller-ui-format-temperature = { TOSTRING($value, "F1") } k
fusion-reactor-controller-ui-format-pressure = { TOSTRING($value, "F1") } pa

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