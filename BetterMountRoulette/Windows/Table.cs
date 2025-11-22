using System;
using System.Collections.Generic;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace BetterMountRoulette.Windows;

public abstract class Table : IDrawable
{
    public abstract string[] OrderedColumnIds { get; }

    protected delegate void SetupColumn();

    protected delegate void DrawColumnCallback();

    protected abstract ImRaii.IEndObject BeginTable();

    protected abstract Dictionary<string, SetupColumn> GetSetupColumns();

    protected abstract IEnumerable<Row> GetRows();

    public virtual void Draw()
    {
        using (BeginTable())
        {
            var setupColumns = GetSetupColumns();

            foreach (var columnId in OrderedColumnIds)
            {
                var column = setupColumns.GetValueOrDefault(columnId);
                
                setupColumns[columnId]();
            }

            ImGui.TableHeadersRow();

            foreach (var row in GetRows())
            {
                ImGui.TableNextRow();

                using (row.Id is null ? ImRaii.PushId(row.Id) : null)
                {
                    foreach (var columnId in OrderedColumnIds)
                    {
                        ImGui.TableNextColumn();

                        if (row.Columns.TryGetValue(columnId, out var columnCallback))
                        {
                            columnCallback();
                        }
                    }
                }
            }
        }
    }

    protected record Row(Dictionary<string, DrawColumnCallback> Columns, string? Id = null);
}
