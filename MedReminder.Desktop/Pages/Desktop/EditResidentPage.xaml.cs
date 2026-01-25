using MedReminder.Models;
using MedReminder.Services.Abstractions;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MedReminder.Pages.Desktop
{
    [QueryProperty(nameof(ResidentId), "id")]
    [QueryProperty(nameof(RoomNumber), "roomNumber")]
    [QueryProperty(nameof(RoomType), "roomType")]
    public partial class EditResidentPage : ContentPage
    {
        private readonly IResidentService _residentService;

        public int ResidentId { get; set; }
        public string? RoomNumber { get; set; }
        public string? RoomType { get; set; }


        public Resident WorkingCopy { get; private set; } = new();

        private bool _isNew;

        public EditResidentPage(IResidentService residentService)
        {
            InitializeComponent();
            _residentService = residentService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (ResidentId > 0)
            {
                var list = await _residentService.LoadAsync();
                var existing = list.FirstOrDefault(r => r.Id == ResidentId);

                if (existing is null)
                {
                    _isNew = true;
                    WorkingCopy = CreateNewResidentWithDefaults();

                    // If we navigated from FloorPlan, prefill room info
                    ApplyRoomPrefillIfProvided();
                }
                else
                {
                    _isNew = false;

                    WorkingCopy = new Resident
                    {
                        Id = existing.Id,

                        // Personal Info
                        FirstName = existing.FirstName,
                        LastName = existing.LastName, 
                        DOB = existing.DOB,
                        SIN = existing.SIN,
                        Gender = existing.Gender,

                        // Address
                        Address = existing.Address,
                        City = existing.City,
                        Province = existing.Province,
                        PostalCode = existing.PostalCode,

                        // Room placement (Floor plan)
                        RoomNumber = existing.RoomNumber,
                        RoomType = existing.RoomType,
                        BedLabel = existing.BedLabel,

                        // Emergency Contacts
                        EmergencyContactName1 = existing.EmergencyContactName1,
                        EmergencyContactPhone1 = existing.EmergencyContactPhone1,
                        EmergencyRelationship1 = existing.EmergencyRelationship1,

                        EmergencyContactName2 = existing.EmergencyContactName2,
                        EmergencyContactPhone2 = existing.EmergencyContactPhone2,
                        EmergencyRelationship2 = existing.EmergencyRelationship2,

                        // Doctor
                        DoctorName = existing.DoctorName,
                        DoctorContact = existing.DoctorContact,

                        // Allergies & Remarks
                        AllergyPeanuts = existing.AllergyPeanuts,
                        AllergyTreeNuts = existing.AllergyTreeNuts,
                        AllergyMilk = existing.AllergyMilk,
                        AllergyEggs = existing.AllergyEggs,
                        AllergyShellfish = existing.AllergyShellfish,
                        AllergyFish = existing.AllergyFish,
                        AllergyWheat = existing.AllergyWheat,
                        AllergySoy = existing.AllergySoy,
                        AllergyLatex = existing.AllergyLatex,
                        AllergyPenicillin = existing.AllergyPenicillin,
                        AllergySulfa = existing.AllergySulfa,
                        AllergyAspirin = existing.AllergyAspirin,
                        AllergyOtherItems = existing.AllergyOtherItems,
                        Remarks = existing.Remarks
                    };

                    WorkingCopy.AllergyNone = !WorkingCopy.AllergyPeanuts && !WorkingCopy.AllergyTreeNuts &&
                                     !WorkingCopy.AllergyMilk && !WorkingCopy.AllergyEggs &&
                                     !WorkingCopy.AllergyShellfish && !WorkingCopy.AllergyFish &&
                                     !WorkingCopy.AllergyWheat && !WorkingCopy.AllergySoy &&
                                     !WorkingCopy.AllergyLatex && !WorkingCopy.AllergyPenicillin &&
                                     !WorkingCopy.AllergySulfa && !WorkingCopy.AllergyAspirin && 
                                     string.IsNullOrWhiteSpace(WorkingCopy.AllergyOtherItems);

                    // In case the user opened edit from FloorPlan for an occupied room,
                    // do NOT overwrite existing room fields.
                    EnsureRelationshipDefaults();
                }
            }
            else
            {
                _isNew = true;
                WorkingCopy = CreateNewResidentWithDefaults();

                // If we navigated from FloorPlan, prefill room info
                ApplyRoomPrefillIfProvided();
            }

            BindingContext = WorkingCopy;
            RefreshAllergyUI();
        }

        private void OnNoneCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            // When "None" is checked (e.Value == true)
            if (WorkingCopy == null) return;

            // When "None" is checked: clear all other allergy fields and disable inputs
            if (e.Value)
            {
                ClearAllAllergies();
            }

            RefreshAllergyUI();
        }

        private void OnSpecificAllergyCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (WorkingCopy == null) return;

            if (e.Value && WorkingCopy.AllergyNone)
            {
                WorkingCopy.AllergyNone = false;
                RefreshAllergyUI();
            }
        }

        // OPTIONAL BUT RECOMMENDED:
        // Hook this to "Other" Entry TextChanged so typing auto turns off "None".
        private void OnOtherAllergyTextChanged(object sender, TextChangedEventArgs e)
        {
            if (WorkingCopy == null) return;

            if (!string.IsNullOrWhiteSpace(e.NewTextValue) && WorkingCopy.AllergyNone)
            {
                WorkingCopy.AllergyNone = false;
                RefreshAllergyUI();
            }
        }

        private void ClearAllAllergies()
        {
            if (WorkingCopy == null) return;

            WorkingCopy.AllergyPeanuts = false;
            WorkingCopy.AllergyTreeNuts = false;
            WorkingCopy.AllergyMilk = false;
            WorkingCopy.AllergyEggs = false;
            WorkingCopy.AllergyShellfish = false;
            WorkingCopy.AllergyFish = false;
            WorkingCopy.AllergyWheat = false;
            WorkingCopy.AllergySoy = false;
            WorkingCopy.AllergyLatex = false;
            WorkingCopy.AllergyPenicillin = false;
            WorkingCopy.AllergySulfa = false;
            WorkingCopy.AllergyAspirin = false;
            WorkingCopy.AllergyOtherItems = string.Empty;
        }

        private void RefreshAllergyUI()
        {
            // Re-bind to ensure the checkboxes update their Enabled/Disabled states
            var temp = BindingContext;
            BindingContext = null;
            BindingContext = temp;
        }

        private Resident CreateNewResidentWithDefaults()
        {
            return new Resident
            {
                EmergencyRelationship1 = "Select Relationship",
                EmergencyRelationship2 = "Select Relationship"
            };
        }

        private void ApplyRoomPrefillIfProvided()
        {
            // Only apply when creating a NEW resident
            if (!_isNew)
                return;

            if (!string.IsNullOrWhiteSpace(RoomNumber))
                WorkingCopy.RoomNumber = RoomNumber;

            if (!string.IsNullOrWhiteSpace(RoomType))
                WorkingCopy.RoomType = RoomType;

            EnsureRelationshipDefaults();
        }

        private void EnsureRelationshipDefaults()
        {
            if (string.IsNullOrWhiteSpace(WorkingCopy.EmergencyRelationship1))
                WorkingCopy.EmergencyRelationship1 = "Select Relationship";

            if (string.IsNullOrWhiteSpace(WorkingCopy.EmergencyRelationship2))
                WorkingCopy.EmergencyRelationship2 = "Select Relationship";
        }

        private bool ValidateForm()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(WorkingCopy.FirstName))
                errors.Add("First Name");

            if (string.IsNullOrWhiteSpace(WorkingCopy.LastName))
                errors.Add("Last Name");

            if (string.IsNullOrWhiteSpace(WorkingCopy.DOB))
                errors.Add("Date of Birth (DOB)");

            if (string.IsNullOrWhiteSpace(WorkingCopy.EmergencyContactName1))
                errors.Add("Emergency Contact 1 Name");

            if (string.IsNullOrWhiteSpace(WorkingCopy.EmergencyContactPhone1))
                errors.Add("Emergency Contact 1 Phone");

            if (string.IsNullOrWhiteSpace(WorkingCopy.EmergencyRelationship1) ||
                WorkingCopy.EmergencyRelationship1 == "Select Relationship")
                errors.Add("Emergency Contact 1 Relationship");

            if (!string.IsNullOrWhiteSpace(WorkingCopy.EmergencyContactName2) &&
                (string.IsNullOrWhiteSpace(WorkingCopy.EmergencyRelationship2) ||
                 WorkingCopy.EmergencyRelationship2 == "Select Relationship"))
            {
                errors.Add("Emergency Contact 2 Relationship (required when name is provided)");
            }

            if (string.IsNullOrWhiteSpace(WorkingCopy.DoctorName))
                errors.Add("Doctor Name");

            // Allergy Validation Logic
            bool hasAnySpecificAllergy = WorkingCopy.AllergyPeanuts || WorkingCopy.AllergyTreeNuts ||
                                         WorkingCopy.AllergyMilk || WorkingCopy.AllergyEggs ||
                                         WorkingCopy.AllergyShellfish || WorkingCopy.AllergyFish ||
                                         WorkingCopy.AllergyWheat || WorkingCopy.AllergySoy ||
                                         WorkingCopy.AllergyLatex || WorkingCopy.AllergyPenicillin ||
                                         WorkingCopy.AllergySulfa || WorkingCopy.AllergyAspirin || !string.IsNullOrWhiteSpace(WorkingCopy.AllergyOtherItems);

            if (!WorkingCopy.AllergyNone && !hasAnySpecificAllergy)
            {
                errors.Add("Allergies (Must select 'None' or check specific allergies)");
            }

            if (errors.Count > 0)
            {
                DisplayAlert("Missing Required Fields", "Please fill in:\n• " + string.Join("\n• ", errors), "OK");
                return false;
            }
            return true;
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (!ValidateForm())
                return;

            await _residentService.UpsertAsync(WorkingCopy);
            await DisplayAlert("Saved", "Resident information saved.", "OK");

            await Shell.Current.GoToAsync($"//{nameof(ResidentsPage)}");
        }

        private async void OnSaveAndAddMedClicked(object sender, EventArgs e)
        {
            if (!ValidateForm())
                return;

            await _residentService.UpsertAsync(WorkingCopy);

            if (WorkingCopy.Id <= 0)
            {
                var list = await _residentService.LoadAsync();
                var latest = list.OrderByDescending(r => r.Id).FirstOrDefault();
                if (latest != null)
                    WorkingCopy.Id = latest.Id;
            }

            try
            {
                var parameters = new Dictionary<string, object?>
                {
                    ["Item"] = null,
                    ["residentId"] = WorkingCopy.Id,
                    ["residentName"] = WorkingCopy.FullName
                };

                await Shell.Current.GoToAsync(nameof(EditMedicationPage), true, parameters);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Navigation error", ex.Message, "OK");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync($"//{nameof(ResidentsPage)}");
        }
    }
}
