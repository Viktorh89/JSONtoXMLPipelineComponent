using Microsoft.BizTalk.Component.Interop;
using Microsoft.BizTalk.Message.Interop;
using Microsoft.Samples.BizTalk.Pipelines.CustomComponent;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace iBiz.BizTalk.PipelineComponents
{
    [ComponentCategory(CategoryTypes.CATID_PipelineComponent)]
    [System.Runtime.InteropServices.Guid("BF2D7726-FAA3-4062-B6B5-1EAA285171EE")]
    [ComponentCategory(CategoryTypes.CATID_Any)]
    public class JSONToXML : IComponent, IBaseComponent,
                                        IPersistPropertyBag, IComponentUI
    {
       private readonly ResourceManager resourceManager =
          new ResourceManager(
              "iBiz.BizTalk.PipelineComponents.JSONToXML",
              Assembly.GetExecutingAssembly());   

        #region Implementation of IBaseComponent

        [System.ComponentModel.Browsable(false)]
        public string Name
        {
            get { return "JSONToXML"; }
        }

        [System.ComponentModel.Browsable(false)]
        public string Version
        {
            get { return "1.0.0.0"; }
        }

        [System.ComponentModel.Browsable(false)]
        public string Description
        {
            get
            {
                return "This Component converts incoming JSON to XML";
            }
        }

        #endregion
       

        [System.ComponentModel.Description("Namespace for message")]
        public string NameSpace { get; set; }

        [System.ComponentModel.Description("Root node for message")]
        public string RootNode { get; set; }
      

        #region Implementation of IComponent

        public IBaseMessage Execute(IPipelineContext pContext, IBaseMessage pInMsg)
        {
            IBaseMessageContext messageContext = pInMsg.Context;
            var inboundStream = pInMsg.BodyPart.GetOriginalDataStream();

            // Check if source stream can seek
            if (!inboundStream.CanSeek)
            {
                // Create a virtual (seekable) stream
                SeekableReadOnlyStream seekableStream = new SeekableReadOnlyStream(inboundStream);

                // Replace sourceStream with a new seekable stream wrapper
                inboundStream = pInMsg.BodyPart.Data;
            }
            // www.quicklearn.com/blog/2013/09/06/biztalk-server-2013-support-for-restful-services-part-45/
            // Please don't use this code for production purposes with large messages.
            // You have been warned.

            // I am not going to assume any specific encoding. The BodyPart tells us
            // which encoding to use, so we will use that to get the string -- except
            // when it doesn't, then I will be a horrible person and assume an encoding.
            using (var sr = new StreamReader(inboundStream))
            {
                var Rootnode = RootNode;
                var DEFAULT_PREFIX = "ns0";
                var Namespace = NameSpace;

                var jsonString = sr.ReadToEnd();

                var rawDoc = JsonConvert.DeserializeXmlNode(jsonString, string.IsNullOrWhiteSpace(Rootnode) ? "NO_NODE" :Rootnode, true);

                // Here we are ensuring that the custom namespace shows up on the root node
                // so that we have a nice clean message type on the request messages
                var xmlDoc = new XmlDocument();
                xmlDoc.AppendChild(xmlDoc.CreateElement(DEFAULT_PREFIX, rawDoc.DocumentElement.LocalName, Namespace));
                xmlDoc.DocumentElement.InnerXml = rawDoc.DocumentElement.InnerXml;

                // All of the heavy lifting has been done, now we just have to shuffle this
                // new data over to the output message
                writeMessage(pInMsg, xmlDoc);
            }
            return pInMsg;
        }

        private static void writeMessage(Microsoft.BizTalk.Message.Interop.IBaseMessage inmsg, XmlDocument xmlDoc)
        {
            var outputStream = new VirtualStream();

            using (var writer = XmlWriter.Create(outputStream, new XmlWriterSettings()
            {
                CloseOutput = false,
                Encoding = Encoding.UTF8
            }))
            {
                xmlDoc.WriteTo(writer);
                writer.Flush();
            }

            outputStream.Seek(0, SeekOrigin.Begin);

            inmsg.BodyPart.Charset = Encoding.UTF8.WebName;
            inmsg.BodyPart.Data = outputStream;
        }

        #endregion

        #region Implementation of IPersistPropertyBag

        #region Utility Functionality

        private static string ToStringOrDefault(object property, string defaultValue)
        {
            if (property != null)
            {
                return property.ToString();
            }

            return defaultValue;
        }

        /// <summary>
        /// Reads property value from property bag
        /// </summary>
        /// <param name="pb">Property bag</param>
        /// <param name="propName">Name of property</param>
        /// <returns>Value of the property</returns>
        private object ReadPropertyBag(IPropertyBag pb, string propName)
        {
            object val = null;
            try
            {
                pb.Read(propName, out val, 0);
            }
            catch (ArgumentException)
            {
                return val;
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message);
            }
            return val;
        }

        /// <summary>
        /// Writes property values into a property bag.
        /// </summary>
        /// <param name="pb">Property bag.</param>
        /// <param name="propName">Name of property.</param>
        /// <param name="val">Value of property.</param>
        private void WritePropertyBag(IPropertyBag pb, string propName, object val)
        {
            try
            {
                pb.Write(propName, ref val);
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message);
            }
        }
        #endregion

        public void GetClassID(out Guid classID)
        {
            classID = new Guid("BF2D7726-FAA3-4062-B6B5-1EAA285171EE");
        }

        public void InitNew()
        {
            throw new NotImplementedException();
        }

        public void Load(IPropertyBag propertyBag, int errorLog)
        {

            if (string.IsNullOrEmpty(NameSpace))
            {
                NameSpace = ToStringOrDefault(ReadPropertyBag(propertyBag, "NameSpace"), string.Empty);
            }
            if (string.IsNullOrEmpty(RootNode))
            {
                RootNode = ToStringOrDefault(ReadPropertyBag(propertyBag, "RootNode"), string.Empty);
            }
         
        }

        public void Save(IPropertyBag propertyBag, bool clearDirty, bool saveAllProperties)
        {
            WritePropertyBag(propertyBag, "NameSpace", NameSpace);
            WritePropertyBag(propertyBag, "RootNode", RootNode);
        }

        #endregion

        #region Implementation of IComponentUI

        public IEnumerator Validate(object projectSystem)
        {
            return null;
        }

        public IntPtr Icon
        {
            get { throw new NotImplementedException(); }
        }

        #endregion
    }
}

