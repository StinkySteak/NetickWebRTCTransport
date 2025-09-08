mergeInto(LibraryManager.library, {

    peerConnection: null,
    dataChannel: null,
    dataChannelReliable: null,

    offerJson: null,
    answerJson: null,

    opCreateOffer: null,
    opCreateOfferDone: null,

    opCreateAnswer: null,
    opCreateAnswerDone: null,

    opSetLocalDescription: null,
    opSetLocalDescriptionDone: null,

    opSetRemoteDescription: null,
    opSetRemoteDescriptionDone: null,

    onMessageCallback: null,

    onIceConnectionStateChangeCallback: null,
    onIceCandidateCallback: null,
    onIceCandidateGathertingStateCallback: null,

    onDataChannelOpenCallback: null,
    onDataChannelCreatedCallback: null,

    onDataChannelReliableOpenCallback: null,
    onDataChannelReliableCreatedCallback: null,


    WebRTC_Unsafe_CreateRTCPeerConnection: function (configJson) {

        const json = UTF8ToString(configJson);

        let config = JSON.parse(json);

        this.peerConnection = new RTCPeerConnection(config);

        this.peerConnection.onicegatheringstatechange = (event) => {
            if (this.onIceCandidateGathertingStateCallback) {

                let stateNumber = 0;

                switch (this.peerConnection.iceGatheringState) {
                    case "new":
                        stateNumber = 0;
                        break;
                    case "gathering":
                        stateNumber = 1;
                        break;
                    case "complete":
                        stateNumber = 2;
                        break;
                }

                Module.dynCall_vi(this.onIceCandidateGathertingStateCallback, stateNumber);
            }
        };

        this.peerConnection.onicecandidate = (event) => {

            console.log(event.candidate);

            if (this.onIceCandidateCallback)
                Module.dynCall_v(this.onIceCandidateCallback);
        };

        this.peerConnection.oniceconnectionstatechange = (event) => {
            if (this.onIceConnectionStateChangeCallback)
                Module.dynCall_v(this.onIceConnectionStateChangeCallback);
        };

        this.peerConnection.ondatachannel = (event) => {

            if (event.channel.label == "sendChannel") {
                this.dataChannel = event.channel;

                if (this.onDataChannelCreatedCallback) {
                    Module.dynCall_v(this.onDataChannelCreatedCallback);
                }


                this.dataChannel.onopen = (event) => {
                    if (this.onDataChannelOpenCallback)
                        Module.dynCall_v(this.onDataChannelOpenCallback);
                };

                this.dataChannel.onmessage = (event) => {
                    if (event.data instanceof ArrayBuffer) {
                        let array = new Uint8Array(event.data);
                        let arrayLength = array.length;

                        var ptr = Module._malloc(arrayLength);

                        let dataBuffer = new Uint8Array(HEAPU8.buffer, ptr, arrayLength);

                        Module.HEAPU8.set(dataBuffer, ptr);

                        dataBuffer.set(array);

                        if (this.onMessageCallback)
                            Module.dynCall_vii(this.onMessageCallback, ptr, dataBuffer.length);
                    }
                };
            }
            else if (event.channel.label == "sendChannelReliable") {
                this.dataChannelReliable = event.channel;

                if (this.onDataChannelReliableCreatedCallback) {
                    Module.dynCall_v(this.onDataChannelReliableCreatedCallback);
                }


                this.dataChannelReliable.onopen = (event) => {
                    if (this.onDataChannelReliableOpenCallback)
                        Module.dynCall_v(this.onDataChannelReliableOpenCallback);
                };

                this.dataChannelReliable.onmessage = (event) => {
                    if (event.data instanceof ArrayBuffer) {
                        let array = new Uint8Array(event.data);
                        let arrayLength = array.length;

                        var ptr = Module._malloc(arrayLength);

                        let dataBuffer = new Uint8Array(HEAPU8.buffer, ptr, arrayLength);

                        Module.HEAPU8.set(dataBuffer, ptr);

                        dataBuffer.set(array);

                        if (this.onMessageCallback)
                            Module.dynCall_vii(this.onMessageCallback, ptr, dataBuffer.length);
                    }
                };
            }
        };
    },

    WebRTC_OnMessage: function (event) {
        if (event.data instanceof ArrayBuffer) {
            let array = new Uint8Array(event.data);
            let arrayLength = array.length;

            var ptr = Module._malloc(arrayLength);

            let dataBuffer = new Uint8Array(HEAPU8.buffer, ptr, arrayLength);

            Module.HEAPU8.set(dataBuffer, ptr);

            dataBuffer.set(array);

            if (this.onMessageCallback)
                Module.dynCall_vii(this.onMessageCallback, ptr, dataBuffer.length);
        }
    },

    WebRTC_Unsafe_GetConnectionState: function () {

        let connectionState = this.peerConnection.connectionState;

        const pointer = _malloc(connectionState.length + 1); // +1 for null terminator
        stringToUTF8(connectionState, pointer, connectionState.length + 1);
        return pointer;
    },

    WebRTC_IsConnectionOpen: function () {
        if (this.dataChannel == null)
            return false;

        if (this.dataChannel.readyState === "open")
            return true;

        return false;
    },

    WebRTC_DataChannelSend: function (dataPointer, dataLength) {
        const byteArray = new Uint8Array(Module.HEAPU8.buffer, dataPointer, dataLength);

        this.dataChannel.send(byteArray);
    },

    WebRTC_DataChannelReliableSend: function (dataPointer, dataLength) {
        const byteArray = new Uint8Array(Module.HEAPU8.buffer, dataPointer, dataLength);

        this.dataChannelReliable.send(byteArray);
    },

    WebRTC_GetOpCreateOfferIsDone: function () {
        return this.opCreateOfferDone;
    },

    WebRTC_GetOpCreateAnswerIsDone: function () {
        return this.opCreateAnswerDone;
    },

    WebRTC_DisposeOpCreateOffer: function () {
        this.opCreateOffer = null;
        this.opCreateOfferDone = false;
    },

    WebRTC_DisposeOpCreateAnswer: function () {
        this.opCreateAnswer = null;
        this.opCreateAnswerDone = false;
    },

    WebRTC_HasOpCreateOffer: function () {
        return this.opCreateOffer != null;
    },

    WebRTC_HasOpCreateAnswer: function () {
        return this.opCreateAnswer != null;
    },

    WebRTC_CreateOffer: async function () {
        this.opCreateOffer = this.peerConnection.createOffer();
        let offer = await this.opCreateOffer;
        this.offerJson = JSON.stringify(offer);
        this.opCreateOfferDone = true;
    },

    WebRTC_CreateAnswer: async function () {
        this.opCreateAnswer = this.peerConnection.createAnswer();
        let answer = await this.opCreateAnswer;
        this.answerJson = JSON.stringify(answer);
        this.opCreateAnswerDone = true;
    },

    WebRTC_Unsafe_CreateDataChannel: function (configJson) {

        const json = UTF8ToString(configJson);

        let config = JSON.parse(json);

        this.dataChannel = this.peerConnection.createDataChannel("sendChannel", config);

        this.dataChannel.onopen = (event) => {
            if (this.onDataChannelOpenCallback)
                Module.dynCall_v(this.onDataChannelOpenCallback);
        };

        this.dataChannel.onmessage = (event) => {
            if (event.data instanceof ArrayBuffer) {
                let array = new Uint8Array(event.data);
                let arrayLength = array.length;

                var ptr = Module._malloc(arrayLength);

                let dataBuffer = new Uint8Array(HEAPU8.buffer, ptr, arrayLength);

                Module.HEAPU8.set(dataBuffer, ptr);

                dataBuffer.set(array);

                if (this.onMessageCallback)
                    Module.dynCall_vii(this.onMessageCallback, ptr, dataBuffer.length);
            }
        };
    },

    WebRTC_Unsafe_CreateDataChannelReliable: function () {

        this.dataChannelReliable = this.peerConnection.createDataChannel("sendChannelReliable");

        this.dataChannelReliable.onopen = (event) => {
            if (this.onDataChannelReliableOpenCallback)
                Module.dynCall_v(this.onDataChannelReliableOpenCallback);
        };

        this.dataChannelReliable.onmessage = (event) => {
            if (event.data instanceof ArrayBuffer) {
                let array = new Uint8Array(event.data);
                let arrayLength = array.length;

                var ptr = Module._malloc(arrayLength);

                let dataBuffer = new Uint8Array(HEAPU8.buffer, ptr, arrayLength);

                Module.HEAPU8.set(dataBuffer, ptr);

                dataBuffer.set(array);

                if (this.onMessageCallback)
                    Module.dynCall_vii(this.onMessageCallback, ptr, dataBuffer.length);
            }
        };
    },



    WebRTC_Unsafe_GetOffer: function () {
        const pointer = _malloc(this.offerJson.length + 1); // +1 for null terminator
        stringToUTF8(this.offerJson, pointer, this.offerJson.length + 1);
        return pointer;
    },

    WebRTC_Unsafe_GetAnswer: function () {
        const pointer = _malloc(this.answerJson.length + 1); // +1 for null terminator
        stringToUTF8(this.answerJson, pointer, this.answerJson.length + 1);
        return pointer;
    },

    WebRTC_Unsafe_SetLocalDescription: async function (sdpJson) {

        const json = UTF8ToString(sdpJson);

        const sdp = JSON.parse(json);

        this.opSetLocalDescription = this.peerConnection.setLocalDescription(sdp);

        await this.opSetLocalDescription;

        this.opSetLocalDescriptionDone = true;
    },

    WebRTC_Unsafe_SetRemoteDescription: async function (sdpJson) {

        const json = UTF8ToString(sdpJson);

        const sdp = JSON.parse(json);

        this.opSetRemoteDescription = this.peerConnection.setRemoteDescription(sdp);

        await this.opSetRemoteDescription;

        this.opSetRemoteDescriptionDone = true;
    },

    WebRTC_HasOpSetLocalDescription: function () {
        return this.opSetLocalDescription != null;
    },

    WebRTC_IsOpSetLocalDescriptionDone: function () {
        return this.opSetLocalDescriptionDone;
    },

    WebRTC_DisposeOpSetLocalDescription: function () {
        this.opSetLocalDescription = null;
        this.opSetLocalDescriptionDone = false;
    },

    WebRTC_HasOpSetRemoteDescription: function () {
        return this.opSetRemoteDescription != null;
    },

    WebRTC_IsOpSetRemoteDescriptionDone: function () {
        return this.opSetRemoteDescriptionDone;
    },

    WebRTC_DisposeOpSetRemoteDescription: function () {
        this.opSetRemoteDescription = null;
        this.opSetRemoteDescriptionDone = false;
    },

    WebRTC_Unsafe_GetLocalDescription: function () {

        let localDescription = this.peerConnection.localDescription;
        let localDescriptionJson = JSON.stringify(localDescription);

        const pointer = _malloc(localDescriptionJson.length + 1); // +1 for null terminator
        stringToUTF8(localDescriptionJson, pointer, localDescriptionJson.length + 1);
        return pointer;
    },

    WebRTC_Unsafe_GetRemoteDescription: function () {
        let remoteDescription = this.peerConnection.remoteDescription;
        let remoteDescriptionJson = JSON.stringify(remoteDescription);

        const pointer = _malloc(remoteDescriptionJson.length + 1); // +1 for null terminator
        stringToUTF8(remoteDescriptionJson, pointer, remoteDescriptionJson.length + 1);
        return pointer;
    },

    WebRTC_SetCallbackOnMessage: function (callback) {
        this.onMessageCallback = callback;
    },

    WebRTC_SetCallbackOnIceConnectionStateChange: function (callback) {
        this.onIceConnectionStateChangeCallback = callback;
    },

    WebRTC_SetCallbackOnDataChannelCreated: function (callback) {
        this.onDataChannelCreatedCallback = callback;
    },

    WebRTC_SetCallbackOnDataReliableChannelCreated: function (callback) {
        this.onDataChannelReliableCreatedCallback = callback;
    },

    WebRTC_SetCallbackOnIceCandidate: function (callback) {
        this.onIceCandidateCallback = callback;
    },

    WebRTC_SetCallbackOnIceCandidateGatheringState: function (callback) {
        this.onIceCandidateGathertingStateCallback = callback;
    },

    WebRTC_SetCallbackOnDataChannelOpen: function (callback) {
        this.onDataChannelOpenCallback = callback;
    },
    WebRTC_SetCallbackOnDataChannelReliableOpen: function (callback) {
        this.onDataChannelReliableOpenCallback = callback;
    },

    WebRTC_CloseConnection: function () {
        if (this.dataChannel)
            this.dataChannel.close();

        if (this.dataChannelReliable)
            this.dataChannelReliable.close();

        if (this.peerConnection)
            this.peerConnection.close();
    },

    WebRTC_GetIsPeerConnectionCreated: function () {
        return this.peerConnection != null;
    },

    WebRTC_Reset: function () {
        this.peerConnection = null;
        this.dataChannel = null;

        this.offerJson = null;
        this.answerJson = null;

        this.opCreateOffer = null;
        this.opCreateOfferDone = null;

        this.opCreateAnswer = null;
        this.opCreateAnswerDone = null;

        this.opSetLocalDescription = null;
        this.opSetLocalDescriptionDone = null;

        this.opSetRemoteDescription = null;
        this.opSetRemoteDescriptionDone = null;

        this.onMessageCallback = null;
        this.onIceConnectionStateChangeCallback = null;
        this.onIceCandidateCallback = null;
        this.onIceCandidateGathertingStateCallback = null;

        this.onDataChannelCreatedCallback = null;
        this.onDataChannelOpenCallback = null;

        this.onDataChannelReliableCreatedCallback = null;
        this.onDataChannelReliableOpenCallback = null;
    },

    WebRTC_Unsafe_GetGatheringState: function () {

        if (this.peerConnection.iceGatheringState == null)
            return 0;

        let stateNumber = 0;

        switch (this.peerConnection.iceGatheringState) {
            case "new":
                stateNumber = 0;
                break;
            case "gathering":
                stateNumber = 1;
                break;
            case "complete":
                stateNumber = 2;
                break;
        }

        return stateNumber;
    }
});