﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Mirage.Urbanization.WinForms
{
    public abstract class ToolstripMenuInitializer<TOption>
        where TOption : IToolstripMenuOption
    {
        protected ToolstripMenuInitializer(ToolStripMenuItem targetToopToolStripMenuItem, IEnumerable<TOption> options)
        {
            foreach (var option in options)
            {
                var localOption = option;
                var item = new ToolStripMenuItem(option.Name);
                item.Click += (sender, e) =>
                {
                    foreach (var x in targetToopToolStripMenuItem.DropDownItems.Cast<ToolStripMenuItem>())
                        x.Checked = false;
                    item.Checked = true;
                    _currentOption = localOption;
                    if (OnSelectionChanged != null)
                        OnSelectionChanged(this, new ToolstripMenuOptionChangedEventArgs<TOption>(_currentOption));
                };

                targetToopToolStripMenuItem.DropDownItems.Add(item);
                item.PerformClick();
            }
        }

        public event EventHandler<ToolstripMenuOptionChangedEventArgs<TOption>> OnSelectionChanged;

        private TOption _currentOption;

        public TOption GetCurrentOption() { return _currentOption; }
    }
}