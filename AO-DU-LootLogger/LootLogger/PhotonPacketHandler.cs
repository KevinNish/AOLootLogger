using ExitGames.Client.Photon;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Transport;
using System;
using System.IO;
using System.Net;

namespace LootLogger
{
    internal class PhotonPacketHandler
    {
        private PacketHandler _eventHandler;

        public PhotonPacketHandler(PacketHandler p)
        {
            _eventHandler = p;
        }

        public void PacketHandler(Packet packet)
        {
            Protocol16 protocol = new Protocol16();
            UdpDatagram udp = packet.Ethernet.IpV4.Udp;
            if (udp.SourcePort == 5056 || udp.DestinationPort == 5056)
            {
                BinaryReader binaryReader = new BinaryReader(udp.Payload.ToMemoryStream());
                IPAddress.NetworkToHostOrder(binaryReader.ReadUInt16());
                binaryReader.ReadByte();
                byte b = binaryReader.ReadByte();
                IPAddress.NetworkToHostOrder(binaryReader.ReadInt32());
                IPAddress.NetworkToHostOrder(binaryReader.ReadInt32());
                int num = 12;
                int num2 = 1;
                for (int i = 0; i < b; i++)
                {
                    try
                    {
                        byte b2 = binaryReader.ReadByte();
                        binaryReader.ReadByte();
                        binaryReader.ReadByte();
                        binaryReader.ReadByte();
                        int num3 = IPAddress.NetworkToHostOrder(binaryReader.ReadInt32());
                        IPAddress.NetworkToHostOrder(binaryReader.ReadInt32());
                        switch (b2)
                        {
                            case 4:
                                break;
                            case 7:
                                binaryReader.BaseStream.Position += 4L;
                                num3 -= 4;
                                goto case 6;
                            case 6:
                                {
                                    binaryReader.BaseStream.Position += num2;
                                    byte b3 = binaryReader.ReadByte();
                                    int num4 = num3 - num - 2;
                                    StreamBuffer streamBuffer = new StreamBuffer(binaryReader.ReadBytes(num4));
                                    switch (b3)
                                    {
                                        case 2:
                                            {
                                                OperationRequest operationRequest = protocol.DeserializeOperationRequest(streamBuffer);
                                                _eventHandler.OnRequest(operationRequest.OperationCode, operationRequest.Parameters);
                                                break;
                                            }
                                        case 3:
                                            {
                                                OperationResponse operationResponse = protocol.DeserializeOperationResponse(streamBuffer);
                                                _eventHandler.OnResponse(operationResponse.OperationCode, operationResponse.ReturnCode, operationResponse.Parameters);
                                                break;
                                            }
                                        case 4:
                                            {
                                                EventData eventData = protocol.DeserializeEventData(streamBuffer);
                                                _eventHandler.OnEvent(eventData.Code, eventData.Parameters);
                                                break;
                                            }
                                        default:
                                            binaryReader.BaseStream.Position += num4;
                                            break;
                                    }
                                    break;
                                }
                            default:
                                binaryReader.BaseStream.Position += num3 - num;
                                break;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }
    }
}
