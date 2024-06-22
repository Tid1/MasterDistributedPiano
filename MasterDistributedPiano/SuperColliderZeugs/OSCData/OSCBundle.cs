namespace MasterDistributedPiano.SuperColliderZeugs.OSCData;
/*
 * @author Paul Varcholik / pvarchol@bespokesoftware.org
 * @author Valentin Simonov / http://va.lent.in/
 */

public sealed class OSCBundle : OSCPacket {
        public const string BUNDLE_PREFIX = "#bundle";

        public override bool IsBundle {
            get { return true; }
        }

        public OSCTimeTag TimeStamp {
            get { return timeStamp; }
        }

        private OSCTimeTag timeStamp;

        public IList<OSCBundle> Bundles {
            get {
                List<OSCBundle> bundles = new List<OSCBundle>();
                var count = data.Count;
                for (var i = 0; i < count; i++) {
                    var item = data[i] as OSCBundle;
                    if (item != null) bundles.Add(item);
                }

                return bundles.AsReadOnly();
            }
        }

        public IList<OSCMessage> Messages {
            get {
                List<OSCMessage> messages = new List<OSCMessage>();
                var count = data.Count;
                for (var i = 0; i < count; i++) {
                    var item = data[i] as OSCMessage;
                    if (item != null) messages.Add(item);
                }

                return messages.AsReadOnly();
            }
        }

        public OSCBundle() : this(new OSCTimeTag()) { }

        public OSCBundle(OSCTimeTag timeStamp) : base(BUNDLE_PREFIX) {
            this.timeStamp = timeStamp;
        }

        public override byte[] ToByteArray() {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(ValueToByteArray(address));
            PadNull(bytes);

            bytes.AddRange(ValueToByteArray(timeStamp));

            var count = data.Count;
            for (var i = 0; i < count; i++) {
                var packet = data[i] as OSCPacket;
                if (packet != null) {
                    byte[] packetBytes = packet.ToByteArray();
                    if (packetBytes.Length % 4 != 0) throw new Exception();

                    bytes.AddRange(ValueToByteArray(packetBytes.Length));
                    bytes.AddRange(packetBytes);
                }
            }

            return bytes.ToArray();
        }

        public static new OSCBundle FromByteArray(byte[] data, ref int start, int end) {
            string address = ValueFromByteArray<string>(data, ref start);
            if (address != BUNDLE_PREFIX) throw new ArgumentException();

            OSCTimeTag timeStamp = ValueFromByteArray<OSCTimeTag>(data, ref start);
            OSCBundle bundle = new OSCBundle(timeStamp);

            while (start < end) {
                int length = ValueFromByteArray<int>(data, ref start);
                int packetEnd = start + length;
                bundle.Append(OSCPacket.FromByteArray(data, ref start, packetEnd));
            }

            return bundle;
        }

        // To prevent "ExecutionEngineException: Attempting to JIT compile method" error on iOS we use a non-generic method version.
        public override int Append(object value) {
            if (!(value is OSCPacket)) throw new ArgumentException();

            OSCBundle nestedBundle = value as OSCBundle;
            if (nestedBundle != null) {
                if (nestedBundle.timeStamp < timeStamp)
                    throw new Exception("Nested bundle's timestamp must be >= than parent bundle's timestamp.");
            }

            data.Add(value);

            return data.Count - 1;
        }
    }