using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using _2Virt;

namespace Box2Virt
{
    
    class HttpParser
    {
        public enum HttpBodyType : int
        {
            Unknown = 0,
            ContentType,
            Chunked
        }

        public enum ParseState : short
        {
            HttpVersion,
            HttpStatus,
            HttpHeader,
            HttpBody,
            HttpDone
        }

        public struct HTTPResponse
        {
            public int          status;
            public HttpBodyType encoded;
            public uint         expectedSize;
            public uint         parsedPos;
            public ParseState   ParseState;

            public static HTTPResponse InitResponse()
            {
                HTTPResponse response = new HTTPResponse();
                response.status = 0;
                response.encoded = HttpBodyType.Unknown;
                response.expectedSize = 0;
                response.parsedPos = 0;
                response.ParseState = ParseState.HttpVersion;
                return response;
            }
        }

        private static bool ParseBodyEncoding(ref HTTPResponse response, byte[] buffer, int start, int end)
        {
            if (response.encoded != HttpBodyType.Unknown)
                return true;

            int pos = -1;
            try
            {
                string header = new string(Encoding.ASCII.GetChars(buffer, start, end - start));
                if ((pos = header.IndexOf("Content-Length:", 0)) >= 0)
                {
                    pos += 15; // skip "Content-Length:"
                    int endPos = header.IndexOf("\r\n", pos);
                    response.expectedSize = (uint)Int32.Parse(header.Substring(pos, endPos - pos));
                    response.encoded = HttpBodyType.ContentType;
                    return true;
                }
                if (((pos = header.IndexOf("Transfer-Encoding:")) >= 0)
                    && (header.IndexOf("chunked", pos + 18) > 0))
                {
                    response.encoded = HttpBodyType.Chunked;
                    return true;
                }
            } catch (Exception) {}
            return false;
        }

        private static bool isWhitespace(char c)
        {
            return (c == ' ') || (c == '\t') || (c == '\r') || (c == '\n');
        }

        private static int skipWhitespace(byte[] buffer, int pos, int end)
        {
            while ((pos < end) && isWhitespace((char)buffer[pos]))
                pos++;
            return pos;
        }

        private static int nextWhitespace(byte[] buffer, int pos, int end)
        {
            while ((pos < end) && !isWhitespace((char)buffer[pos]))
                pos++;
            return (pos < end)? pos : -1;
        }

        private static bool isNewline(byte[] buffer, int pos, int end)
        {
            return (pos < (end - 1)) && ((char)buffer[pos++] == '\r') && ((char)buffer[pos] == '\n');
        }

        private static int nextNewline(byte[] buffer, int pos, int end)
        {
            while ((pos < end) && !isNewline(buffer, pos, end))
                pos++;
            return (pos < end) ? pos : -1;
        }

        private static int skipNewline(byte[] buffer, int pos, int end)
        {
            end = end - 1;
            while (pos < end)
            {
                if (((char)buffer[pos++] == '\r') && ((char)buffer[pos++] == '\n'))
                    return pos;
            }
            return -1;
        }

        public static bool IsPartialResponse(HTTPResponse httpStatus)
        {
            return httpStatus.status < 200;
        }

        public static bool ParseHttp(IoStatus response, ref HTTPResponse httpStatus)
        {
            int pos = (int)httpStatus.parsedPos;
            while ((pos >= 0) && (pos < response.size))
            {
                switch (httpStatus.ParseState)
                {
                    case ParseState.HttpVersion:
                        pos = skipWhitespace(response.buffer, (int)httpStatus.parsedPos, (int)response.size);
                        if ((pos = nextWhitespace(response.buffer, pos, (int)response.size)) >= 0)
                        {
                            httpStatus.parsedPos  = (uint)pos;
                            httpStatus.ParseState = ParseState.HttpStatus;
                        }
                        break;
                    case ParseState.HttpStatus:
                        pos = skipWhitespace(response.buffer, pos, (int)response.size);
                        if ((pos = nextWhitespace(response.buffer, pos, (int)response.size)) >= 0)
                        {
                            try
                            {
                                string status = new String(Encoding.ASCII.GetChars(response.buffer, (int)httpStatus.parsedPos, pos - (int)httpStatus.parsedPos));
                                httpStatus.status = Int32.Parse(status);
                            } catch (Exception) {}
                            if ((pos = skipNewline(response.buffer, pos, (int)response.size)) > 0)
                            {
                                httpStatus.parsedPos = (uint)pos;
                                httpStatus.ParseState = ParseState.HttpHeader;
                            }
                        }
                        break;
                    case ParseState.HttpHeader:
                        if (isNewline(response.buffer, pos, (int)response.size))
                        {
                            pos += 2;
                            httpStatus.parsedPos = (uint)pos;
                            httpStatus.ParseState = ParseState.HttpBody;
                            httpStatus.expectedSize += httpStatus.parsedPos;
                            break;
                        }
                        if ((pos = skipNewline(response.buffer, pos, (int)response.size)) > 0)
                        {
                            ParseBodyEncoding(ref httpStatus, response.buffer, (int)httpStatus.parsedPos, pos);
                            httpStatus.parsedPos = (uint)pos;
                        }
                        break;
                    case ParseState.HttpBody:
                        {
                            switch (httpStatus.encoded)
                            {
                                case HttpBodyType.Chunked:
                                    if ((pos = nextNewline(response.buffer, (int)httpStatus.parsedPos, (int)response.size)) >= 0)
                                    {
                                        int chunk = 0;
                                        try
                                        {
                                            string toParse = new string(Encoding.ASCII.GetChars(response.buffer, (int)httpStatus.parsedPos, pos - (int)httpStatus.parsedPos));
                                            chunk = Int32.Parse(toParse, System.Globalization.NumberStyles.HexNumber);
                                            httpStatus.expectedSize = (uint)pos + (uint)chunk + 4; // add the chunk's last \r\n\r\n chars
                                            httpStatus.parsedPos    = (uint)pos + (uint)chunk + 4;
                                        }
                                        catch (Exception) { chunk = -1; }

                                        return (chunk == 0);
                                    }
                                    break;
                                case HttpBodyType.ContentType:
                                case HttpBodyType.Unknown:
                                    break;
                            }

                            return response.size >= httpStatus.expectedSize;
                        }
                }
            }

            return false;
        }
    }
}
