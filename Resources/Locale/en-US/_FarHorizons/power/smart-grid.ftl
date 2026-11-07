guide-entry-smart-grid = Smart Grid

smart-grid-priority-essential-systems = Essential Systems
smart-grid-priority-life-support = Life Support
smart-grid-priority-shuttle-operations = Shuttle Operations
smart-grid-priority-lighting = Lighting
smart-grid-priority-general = General

smart-grid-source-health-healthy = Healthy
smart-grid-source-health-constrained = Constrained
smart-grid-source-health-output-capped = Output capped
smart-grid-source-health-low-reserve = Low reserve
smart-grid-source-health-disabled = Disabled

smart-grid-transition-stable = Stable
smart-grid-transition-shedding = Shedding pending
smart-grid-transition-recovering = Recovery pending

smart-grid-availability-header = Smart grid availability
smart-grid-availability-input = Availability received from the input network
smart-grid-availability-output = Availability sent to the output network
smart-grid-availability-markers = The left `->` is received from the input network, the right `->` is sent to the output network, and a solid outline is a manual override.
smart-grid-availability-select-essential-systems = Select Essential Systems availability. This enables containment emitters, the AI core, and cyborg chargers.
smart-grid-availability-select-life-support = Select Life Support availability. This enables atmospheric equipment, airlocks, cryogenic tubes, stasis beds, and surgical equipment.
smart-grid-availability-select-shuttle-operations = Select Shuttle Operations availability. This enables shuttle controls, engines, gyros, and cameras.
smart-grid-availability-select-lighting = Select Lighting availability. This enables standard lighting.
smart-grid-availability-select-general = Select General availability. This enables all devices.
smart-grid-availability-insufficient-access = Insufficient access.

smart-grid-battery-mode-reserve = Reserve Mode
smart-grid-battery-mode-reserve-tooltip = This device is receiving a smart grid control signal from a battery above it in the network. Device charge is reserved for ramp-up smoothing and supporting overrides.
smart-grid-battery-mode-primary = Primary Mode
smart-grid-battery-mode-primary-tooltip = This device has no batteries above it in the network. It will actively use its battery reserve to increase the network's availability level in an attempt to sustain operations and provide load to trigger generator spool-up.
smart-grid-battery-meter-percent = { $percent }
smart-grid-battery-meter-energy = { $energy } Wh
smart-grid-battery-band-tooltip =
    { $priority }
    Shed: { $shed }
    Recover: { $recovery }
smart-grid-battery-band-tooltip-insufficient-access =
    { $details }
    { smart-grid-availability-insufficient-access }

cable-multitool-system-smart-grid = Smart Grid Availability: { $availability } (local: { $localAvailability })
                                    Source Health: { $sourceHealth }
                                    Transition: { $transition }
cable-multitool-system-smart-grid-unavailable = Smart Grid: This network is not participating in smart-grid evaluation.
cable-multitool-system-smart-grid-sources = Immediate Sources:
                                            { $sources }
cable-multitool-system-smart-grid-source = - { $name }: { POWERWATTS($output) } / { POWERWATTS($maximumOutput) } ({ $health })
cable-multitool-system-smart-grid-battery-source = - { $name }: { POWERWATTS($output) } / { POWERWATTS($maximumOutput) }; { POWERJOULES($storage) } / { POWERJOULES($capacity) } ({ $health })
cable-multitool-system-smart-grid-no-sources = None

power-monitoring-window-smart-grid = Smart Grid
power-monitoring-window-smart-grid-status = Availability: { $availability } (local: { $localAvailability })
                                           Source health: { $sourceHealth }
                                           Transition: { $transition }
