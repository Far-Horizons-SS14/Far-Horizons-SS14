fusion-reactor-battery-ui-power-format = { POWERWATTS($power) }

fusion-reactor-battery-ui-label-input = IN
fusion-reactor-battery-ui-label-output = OUT

fusion-reactor-battery-ui-external = External
fusion-reactor-battery-ui-internal = Internal

fusion-reactor-battery-ui-max = Max:
fusion-reactor-battery-ui-current = Current:

fusion-reactor-battery-ui-storage = Storage
fusion-reactor-battery-ui-stored = Stored:
fusion-reactor-battery-ui-energy = Energy:

fusion-reactor-battery-ui-charge-percent = { TOSTRING($charge, "P1") }

fusion-reactor-battery-ui-eta-full = ETA (full):
fusion-reactor-battery-ui-eta-empty = ETA (empty):
fusion-reactor-battery-ui-eta-value = ~{ $minutes } min
fusion-reactor-battery-ui-eta-value-max = >{ $minutes } min
fusion-reactor-battery-ui-eta-value-na = N/A

fusion-reactor-battery-fmt-joules = { TOSTRING($divided, "F1") } { $places ->
    [0] J
    [1] kJ
    [2] MJ
    [3] GJ
    [4] TJ
    *[5] ???
}