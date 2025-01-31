// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2022 Datadog, Inc.

#include "EtwEventsHandler.h"
#include "Protocol.h"
#include "IpcClient.h"
#include "../../Datadog.Profiler.Native/ClrEventsParser.h"
#include <iomanip>
#include <iostream>
#include <sstream>


EtwEventsHandler::EtwEventsHandler()
    :
    _showMessages {false},
    _pReceiver {nullptr}
{
}

EtwEventsHandler::EtwEventsHandler(IIpcLogger* logger, IEtwEventsReceiver* pClrEventsReceiver)
    :
    _logger {logger},
    _pReceiver {pClrEventsReceiver}
{
}

EtwEventsHandler::~EtwEventsHandler()
{
    Stop();
}

void EtwEventsHandler::Stop()
{
    _stopRequested.store(true);
}

void EtwEventsHandler::OnStartError()
{
    Stop();
}

void EtwEventsHandler::OnConnectError()
{
    Stop();
}

void EtwEventsHandler::WriteSuccessResponse(HANDLE hPipe)
{
    DWORD written;
    if (!::WriteFile(hPipe, &SuccessResponse, sizeof(SuccessResponse), &written, nullptr))
    {
        _logger->Warn("Failed to send success response\n");
    }
}

void EtwEventsHandler::OnConnect(HANDLE hPipe)
{
    const DWORD bufferSize = (1 << 16) + sizeof(IpcHeader);
    auto buffer = std::make_unique<uint8_t[]>(bufferSize);
    auto message = reinterpret_cast<ClrEventsMessage*>(buffer.get());

    DWORD readSize;
    while (!_stopRequested.load())
    {
        readSize = 0;
        if (!ReadEvents(hPipe, buffer.get(), bufferSize, readSize))
        {
            _logger->Warn("Stop reading events");
            break;
        }

        // check the message based on the expected command
        if (message->CommandId == Commands::IsAlive)
        {
            WriteSuccessResponse(hPipe);
        }
        else
        if (message->CommandId == Commands::ClrEvents)
        {
            if (message->Size > readSize)
            {
                std::stringstream builder;
                builder << "Invalid format: read size " << readSize << " bytes is smaller than supposed message size " << message->Size + sizeof(IpcHeader);
                _logger->Error(builder.str());

                // TODO: maybe we should stop the communication???
                continue;
            }

            const EVENT_HEADER* pHeader = &(message->EtwHeader);
            uint32_t tid = pHeader->ThreadId;
            uint8_t version = pHeader->EventDescriptor.Version;
            uint64_t keyword = pHeader->EventDescriptor.Keyword;
            uint8_t level = pHeader->EventDescriptor.Level;
            uint16_t id = pHeader->EventDescriptor.Id;
            uint64_t timestamp = pHeader->TimeStamp.QuadPart;

            ClrEventPayload* pPayload = (ClrEventPayload*)(&(message->Payload));
            uint16_t userDataLength = pPayload->EtwUserDataLength;
            uint8_t* pUserData = (uint8_t*)((byte*)&(pPayload->EtwPayload));

            if ((keyword == KEYWORD_GC) && (id == EVENT_ALLOCATION_TICK))
            {
                // TODO: set a breakpoint here to check the content of the payload where the type name should be visible
            }

            if (_pReceiver != nullptr)
            {
                _pReceiver->OnEvent(timestamp, tid, version, keyword, level, id, userDataLength, pUserData);
            }

            // fire and forget so no need to answer
            //WriteSuccessResponse(hPipe);
        }
        else
        {
            std::stringstream builder;
            builder << "Invalid command (" << message->CommandId << ")...";
            _logger->Error(builder.str());

            // fire and forget so no need to answer
            //WriteErrorResponse(hPipe);
            break;
        }
    }
}

bool EtwEventsHandler::ReadEvents(HANDLE hPipe, uint8_t* pBuffer, DWORD bufferSize, DWORD& readSize)
{
    bool success = true;
    auto pMessage = reinterpret_cast<ClrEventsMessage*>(pBuffer);
    DWORD totalReadSize = 0;
    DWORD lastError = ERROR_SUCCESS;

    while (totalReadSize < bufferSize)
    {
        success = ::ReadFile(hPipe, &(pBuffer[totalReadSize]), bufferSize - totalReadSize, &readSize, nullptr);

        if (!success || readSize == 0)
        {
            lastError = ::GetLastError();

            if (lastError == ERROR_MORE_DATA)
            {
                totalReadSize += readSize;
                if (totalReadSize == bufferSize)
                {
                    _logger->Error("Named pipe read buffer was too small...");
                    return false;
                }

                continue;
            }
            else
            {
                if (lastError == ERROR_BROKEN_PIPE)
                {
                    _logger->Warn("Disconnected named pipe client...");
                }
                else
                {
                    std::stringstream builder;
                    builder << "Error reading from named pipe (" << lastError << ")...";
                    _logger->Error(builder.str());
                }
                return false;
            }
        }
        else
        {
            if (!IsMessageValid(pMessage))
            {
                _logger->Error("Invalid Magic signature in message from Agent...");

                // fire and forget
                // WriteErrorResponse(hPipe);
                return false;
            }

            return true;
        }
    }

    // too big for the buffer
    _logger->Error("Named pipe read buffer was too small...");
    return false;
}