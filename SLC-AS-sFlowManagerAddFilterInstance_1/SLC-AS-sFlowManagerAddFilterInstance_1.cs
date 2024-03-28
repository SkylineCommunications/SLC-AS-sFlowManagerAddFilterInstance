/*
****************************************************************************
*  Copyright (c) 2024,  Skyline Communications NV  All Rights Reserved.    *
****************************************************************************

By using this script, you expressly agree with the usage terms and
conditions set out below.
This script and all related materials are protected by copyrights and
other intellectual property rights that exclusively belong
to Skyline Communications.

A user license granted for this script is strictly for personal use only.
This script may not be used in any way by anyone without the prior
written consent of Skyline Communications. Any sublicensing of this
script is forbidden.

Any modifications to this script by the user are only allowed for
personal use and within the intended purpose of the script,
and will remain the sole responsibility of the user.
Skyline Communications will not be responsible for any damages or
malfunctions whatsoever of the script resulting from a modification
or adaptation by the user.

The content of this script is confidential information.
The user hereby agrees to keep this confidential information strictly
secret and confidential and not to disclose or reveal it, in whole
or in part, directly or indirectly to any person, entity, organization
or administration without the prior written consent of
Skyline Communications.

Any inquiries can be addressed to:

	Skyline Communications NV
	Ambachtenstraat 33
	B-8870 Izegem
	Belgium
	Tel.	: +32 51 31 35 69
	Fax.	: +32 51 31 01 29
	E-mail	: info@skyline.be
	Web		: www.skyline.be
	Contact	: Ben Vandenberghe

****************************************************************************
Revision History:

DATE		VERSION		AUTHOR			COMMENTS

28/03/2024	1.0.0.1		RDM, Skyline	Initial version
****************************************************************************
*/

using System.Collections.Generic;
using System.IO;
using System.Text;
using System;

namespace SLC_AS_sFlowManagerAddFilterInstance_1
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Skyline.DataMiner.Automation;
    using Messages;

    /// <summary>
    /// Represents a DataMiner Automation script.
    /// </summary>
    public class Script
    {
        /// <summary>
        /// The script entry point.
        /// </summary>
        /// <param name="engine">Link with SLAutomation process.</param>
        public void Run(Engine engine)
        {
            try
            {
                engine.Timeout = TimeSpan.FromMinutes(30);
                engine.SetFlag(RunTimeFlags.NoKeyCaching);

                var addFilter = new AddFilter(engine);
                addFilter.Run();
            }
            catch (InteractiveUserDetachedException)
            {
                engine.ExitSuccess("User detached");
            }
            catch (Exception e)
            {
                engine.ExitFail(String.Format("Adding filter failed: {0}", e.Message));
            }
        }

        public class AddFilter
        {
            private const int FilterDefinitionsTablePID = 2000;
            private const int FilterDefinitionsOverridableFieldsColumnPID = 2003;
            private const int FilterDefinitionsOverridableValuesColumnPID = 2004;
            private const int ExternalRequestPID = 2;

            private Engine engine;
            private IActionableElement sFlowManager;
            private string[] filterDefinitions;
            private Filter filter;
            private string errorMessage;

            public AddFilter(Engine engine)
            {
                this.engine = engine;

                sFlowManager = engine.GetDummy("sFlow Manager");
                filterDefinitions = sFlowManager.GetTablePrimaryKeys(FilterDefinitionsTablePID);

                filter = new Filter();

                errorMessage = "";
            }

            public void Run()
            {
                var action = Actions.Configure;
                while (action != Actions.Finished)
                {
                    action = Execute(action);
                    //do something extra
                }
            }

            private Actions Execute(Actions action)
            {
                switch (action)
                {
                    case Actions.Configure:
                        return Configure();
                    case Actions.Create:
                        return Create();
                }

                return Actions.Finished;
            }
            private Actions Configure()
            {
                var uir = new UIResults();
                var uib = new UIBuilder();
                uib.Height = 575;
                uib.RequireResponse = true;

                var row = 0;

                uib.AppendBlock(new UIBlockDefinition { Type = UIBlockType.StaticText, Text = "Filter Name", Row = row, Column = 0, Width = 150 });
                uib.AppendBlock(new UIBlockDefinition { Type = UIBlockType.TextBox, InitialValue = filter.Name, DestVar = "name", Row = row, Column = 1, Width = 200 });

                uib.AppendBlock(new UIBlockDefinition { Type = UIBlockType.StaticText, Text = "Filter Definition", Row = ++row, Column = 0, Width = 150 });
                if (filterDefinitions.Length == 0)
                {
                    uib.AppendBlock(new UIBlockDefinition { Type = UIBlockType.StaticText, Text = "No available filter definitions", Row = row, Column = 1, Width = 300 });
                }
                else
                {
                    var filterDefinitionDropdown = new UIBlockDefinition { Type = UIBlockType.DropDown, InitialValue = ((filter.FilterDefinition == null) ? "Select Filter Definition" : filter.FilterDefinition.Name), DestVar = "filterDefinition", WantsOnChange = true, Row = row, Column = 1, Width = 200 };
                    if (filter.FilterDefinition == null)
                    {
                        filterDefinitionDropdown.AddDropDownOption("Select Filter Definition");
                        filterDefinitionDropdown.AddDropDownOption("------------------------");
                    }
                    foreach (var filterDefinition in filterDefinitions.OrderBy(f => f))
                        filterDefinitionDropdown.AddDropDownOption(filterDefinition);
                    uib.AppendBlock(filterDefinitionDropdown);

                    if (filter.FilterDefinition != null)
                    {
                        //separator
                        uib.AppendBlock(new UIBlockDefinition { Type = UIBlockType.StaticText, Text = "", Row = ++row, Column = 0, Width = 300, ColumnSpan = 2 });

                        foreach (var field in filter.OverridenValues)
                        {
                            uib.AppendBlock(new UIBlockDefinition { Type = UIBlockType.StaticText, Text = field.Key, Row = ++row, Column = 0, Width = 150 });

                            var possibleValues = filter.FilterDefinition.PossibleValues[field.Key];
                            if (possibleValues == null)
                                uib.AppendBlock(new UIBlockDefinition { Type = UIBlockType.TextBox, InitialValue = field.Value, DestVar = field.Key, Row = row, Column = 1, Width = 200 });
                            else
                            {
                                var fieldDropdown = new UIBlockDefinition { Type = UIBlockType.DropDown, InitialValue = ((field.Value == null) ? "Select Value" : field.Value), DestVar = field.Key, Row = row, Column = 1, Width = 200 };
                                if (field.Value == null)
                                {
                                    fieldDropdown.AddDropDownOption("Select Value");
                                    fieldDropdown.AddDropDownOption("------------");
                                }
                                foreach (var possibleValue in possibleValues.OrderBy(p => p))
                                    fieldDropdown.AddDropDownOption(possibleValue);
                                uib.AppendBlock(fieldDropdown);
                            }
                        }
                    }
                }

                uib.AppendBlock(new UIBlockDefinition { Type = UIBlockType.Button, Text = "Add", DestVar = "add", Row = ++row, Column = 1, Width = 100 });
                uib.AppendBlock(new UIBlockDefinition { Type = UIBlockType.Button, Text = "Cancel", DestVar = "cancel", Row = ++row, Column = 1, Width = 100 });

                uib.AppendBlock(new UIBlockDefinition { Type = UIBlockType.StaticText, Text = errorMessage, Row = ++row, Column = 1, Width = 300 });

                uib.ColumnDefs = "a;a";
                uib.RowDefs = String.Join(";", Enumerable.Repeat("a", row + 1));
                uir = engine.ShowUI(uib);


                filter.Name = uir.GetString("name");
                var filterDefinitionName = uir.GetString("filterDefinition");
                if (!filterDefinitionName.StartsWith("-") && (filter.FilterDefinition == null || filter.FilterDefinition.Name != filterDefinitionName))
                {
                    filter.OverridenValues = new Dictionary<string, string>();
                    filter.FilterDefinition = new FilterDefinition
                    {
                        Name = filterDefinitionName,
                        PossibleValues = new Dictionary<string, string[]>()
                    };

                    var overridableFields = ((string)sFlowManager.GetParameterByPrimaryKey(FilterDefinitionsOverridableFieldsColumnPID, filterDefinitionName)).Split(';');
                    var overridableValues = ((string)sFlowManager.GetParameterByPrimaryKey(FilterDefinitionsOverridableValuesColumnPID, filterDefinitionName)).Split(';');
                    for (int i = 0; i < overridableFields.Length; i++)
                    {
                        var field = overridableFields[i];

                        filter.OverridenValues[field] = null;
                        filter.FilterDefinition.PossibleValues[field] = (overridableValues[i] != null && overridableValues[i] != "-1") ? overridableValues[i].Split(',') : null;
                    }
                }
                else if (filter.OverridenValues != null)
                {
                    foreach (var field in filter.OverridenValues.Keys.ToList())
                    {
                        var selectedValue = uir.GetString(field);
                        if (!String.IsNullOrEmpty(selectedValue) && selectedValue != "Select Value" && !selectedValue.StartsWith("-"))
                            filter.OverridenValues[field] = selectedValue;
                    }
                }

                if (uir.WasButtonPressed("cancel"))
                    return Actions.Finished;
                if (uir.WasButtonPressed("add"))
                {
                    if (filter.IsFullyConfigured())
                        return Actions.Create;
                    else
                        errorMessage = "Can only create filter when everything is configured!";
                }

                return Actions.Configure;
            }
            private Actions Create()
            {
                sFlowManager.SetParameter(ExternalRequestPID, new FilterUpdateMessage
                {
                    Name = filter.Name,
                    Definition = filter.FilterDefinition.Name,
                    OverriddenValues = filter.OverridenValues

                });

                return Actions.Finished;
            }

            private enum Actions { Configure, Create, Finished }
        }

        public class Filter
        {
            public string Name { get; set; }
            public FilterDefinition FilterDefinition { get; set; }
            public Dictionary<string, string> OverridenValues { get; set; }

            public bool IsFullyConfigured()
            {
                return !String.IsNullOrEmpty(Name) && FilterDefinition != null && !OverridenValues.Any(v => String.IsNullOrEmpty(v.Value) || v.Value == "Select");
            }
        }

        public class FilterDefinition
        {
            public string Name { get; set; }
            public Dictionary<string, string[]> PossibleValues { get; set; }
        }
    }
}

namespace Messages
{
    public class FilterUpdateMessage
    {
        public string Command = "FilterUpdateMessage";

        public string Name { get; set; }

        public string Definition { get; set; }

        public Dictionary<string, string> OverriddenValues { get; set; }
    }
}