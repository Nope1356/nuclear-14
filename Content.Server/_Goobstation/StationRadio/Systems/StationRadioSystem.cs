using Content.Goobstation.Common.DeviceNetwork;
using Content.Goobstation.Shared.StationRadio.Components;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Goobstation.StationRadio.Systems;

public sealed class StationRadioSystem : EntitySystem
{
    // Vinyl -> Server -> Receivers commands
    public const string PlayAudioCommand = "station_radio_play_audio";
    public const string StopAudioCommand = "station_radio_stop_audio";
    public const string SetAudioStateCommand = "station_radio_set_audio_state";

    // Request that is sent backwards from a receiver to vinyl player to get info about the audio.
    // In a perfect world this wouldn't
    public const string AudioRequestCommand = "station_radio_request_audio";

    // Network address of an entity
    public const string AudioRequestAddressData = "station_radio_request_audio";

    public const string AudioPathData = "station_radio_data_audio_path";
    public const string AudioPlaybackData = "station_radio_data_audio_playback";
    public const string AudioStateData = "station_radio_data_audio_state";

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DeviceNetworkSystem _device = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationRadioServerComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<StationRadioServerComponent, DeviceNetworkPacketEvent>(OnServerRelay);
        SubscribeLocalEvent<StationRadioServerComponent, DeviceNetworkTransmitFrequencyChangedEvent>(OnServerChangeFrequency);
        SubscribeLocalEvent<StationRadioReceiverComponent, DeviceNetworkPacketEvent>(OnReceive);
        SubscribeLocalEvent<StationRadioReceiverComponent, DeviceNetworkReceiveFrequencyChangedEvent>(OnReceiverChangeFrequency);
    }

    private void OnNewLink(Entity<StationRadioServerComponent> ent, ref NewLinkEvent args)
    {
        if (args.SourcePort != ent.Comp.MusicOutputPort)
            return;

        ent.Comp.VinylPlayer = args.Source;
    }

    private void OnServerChangeFrequency(Entity<StationRadioServerComponent> ent, ref DeviceNetworkTransmitFrequencyChangedEvent args)
    {
        // Tell all listeners to stop playing everything
        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = StopAudioCommand
        };
        _device.QueuePacket(ent.Owner, null, payload, args.OldFrequency);
    }

    private void OnReceiverChangeFrequency(Entity<StationRadioReceiverComponent> ent, ref DeviceNetworkReceiveFrequencyChangedEvent args)
    {
        // Send a request to get currently playing music and it's playback position
        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = AudioRequestCommand
        };
        _device.QueuePacket(ent.Owner, null, payload, args.NewFrequency);
    }

    private void OnServerRelay(Entity<StationRadioServerComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        if (args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command))
        {
            switch (command)
            {
                case AudioRequestCommand:
                    break;

            }
        }

        _device.QueuePacket(ent.Owner, null, args.Data);
    }

    private void OnReceive(Entity<StationRadioReceiverComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command))
            return;

        switch (command)
        {
            case PlayAudioCommand:
                if (args.Data.TryGetValue(AudioPathData, out SoundSpecifier? sound))
                    PlayAudio(ent, sound);
                break;
            case StopAudioCommand:
                ent.Comp.SoundEntity = _audio.Stop(ent.Comp.SoundEntity);
                break;
            case SetAudioStateCommand:
                if (args.Data.TryGetValue(AudioStateData, out AudioState state))
                    _audio.SetState(ent.Comp.SoundEntity, state);
                break;
        }
    }

    private void PlayAudio(Entity<StationRadioReceiverComponent> ent, SoundSpecifier? sound)
    {
        // Remove the previous audio entity if it existed
        if (ent.Comp.SoundEntity != null)
            ent.Comp.SoundEntity = _audio.Stop(ent.Comp.SoundEntity);

        var audio = _audio.PlayPvs(sound,
            ent.Owner,
            ent.Comp.DefaultParams);
        if (audio != null)
            ent.Comp.SoundEntity = audio.Value.Entity;
    }
}
