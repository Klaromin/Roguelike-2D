using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RoomNodeSO : ScriptableObject
{
    public string id;
    public List<string> parentRoomNodeIDList = new List<string>();
    public List<string> childRoomNodeIDList = new List<string>();
    [HideInInspector] public RoomNodeGraphSO roomNodeGraph;
    public RoomNodeTypeSO roomNodeType;
    [HideInInspector] public RoomNodeTypeListSO roomNodeTypeList;

    #region Editor Code
#if UNITY_EDITOR
    [HideInInspector] public Rect rect;
    [HideInInspector] public bool isLeftClickDragging = false;
    [HideInInspector] public bool isSelected = false;

    public void Initialise(Rect rect, RoomNodeGraphSO nodeGraph, RoomNodeTypeSO roomNodeType)
    {
        this.rect = rect;
        this.id = Guid.NewGuid().ToString();
        this.name = "RoomNode";
        this.roomNodeGraph = nodeGraph;
        this.roomNodeType = roomNodeType;
        //Load room node type list
        roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
    }

    public void Draw(GUIStyle nodeStyle)
    {
        //Draw Node Box Using Begin Area
        GUILayout.BeginArea(rect, nodeStyle);
        //Start Region to Detect Popup Selection Changes
        EditorGUI.BeginChangeCheck();

        if (parentRoomNodeIDList.Count > 0 || roomNodeType.isEnterance)
        {
            //Display a label that can't be changed
            EditorGUILayout.LabelField(roomNodeType.roomNodeTypeName); // aþaðýdaki gibi popup ile seçme seçeneði bulundurmak yerine, parentý yok ya da enterance ise Label ile baþlýk veriyoruz
        }
        else
        {
            //Display a popup using the RoomNodeType name values that can be selected from(default to the curerently set roomNodeType)
            int selected = roomNodeTypeList.list.FindIndex(x => x == roomNodeType);
            int selection = EditorGUILayout.Popup("", selected, GetRoomNodeTypesToDisplay());

            roomNodeType = roomNodeTypeList.list[selection];

            //Eðer oda tipi deðiþtiyse child baðlantýlarýný potansiyel olarak kes
            if (roomNodeTypeList.list[selected].isCorridor && !roomNodeTypeList.list[selection].isCorridor || !roomNodeTypeList.list[selected].isCorridor &&
                roomNodeTypeList.list[selection].isCorridor || !roomNodeTypeList.list[selected].isBossRoom && roomNodeTypeList.list[selection].isBossRoom)
            {
                if (childRoomNodeIDList.Count > 0)
                {
                    for (int i = childRoomNodeIDList.Count - 1; i >= 0; i--)
                    {
                        //Get child room node
                        RoomNodeSO childRoomNode = roomNodeGraph.GetRoomNode(childRoomNodeIDList[i]);
                        //if child room node is selected
                        if (childRoomNode != null)
                        {
                            //remove childID from parent room node
                            RemoveChildRoomNodeIDFromRoomNode(childRoomNode.id);
                            //remove parenID from parent room node
                            childRoomNode.RemoveParenRoomNodeIDFromRoomNode(id);



                        }
                    }
                }
            }

        }
        if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(this);

        GUILayout.EndArea();
    }





    

    public string[] GetRoomNodeTypesToDisplay()
    {
        string[] roomArray = new string[roomNodeTypeList.list.Count]; //room node typelist listesinin üye sayýsý kadar bir boþ array oluþturduk.

        for (int i = 0; i < roomNodeTypeList.list.Count; i++)// yine ayný liste üye sayýsý kadar for döngüsü baþlattýk.
        {

            if (roomNodeTypeList.list[i].displayInNodeGraphEditor) // eðer room type bizim display etmek istediðimiz bir type ise
            {
                roomArray[i] = roomNodeTypeList.list[i].roomNodeTypeName; // onu room arrayine ekledik. (RoomNodeTypeSO'da belirtmiþtik hangilerini display etmek istediðimizi)
            }


        }
        return roomArray;
    }

    public void ProcessEvents(Event currentEvent)
    {
        switch (currentEvent.type)
        {
            case EventType.MouseDown:
                ProcessMouseDownEvent(currentEvent);
                break;

            case EventType.MouseUp:
                ProcessMouseUpEvent(currentEvent);
                break;
            case EventType.MouseDrag:
                ProcessMouseDragEvent(currentEvent);
                break;
            default:
                break;
        }

    }
    private void ProcessMouseDownEvent(Event currentEvent)
    {
        if (currentEvent.button == 0) // 0 sol click temsil ediyor.
        {
            ProcessLeftClickDownEvent();
        }
        else if (currentEvent.button == 1)
        {
            ProcessRightClickDownEvent(currentEvent);

        }

    }

    private void ProcessLeftClickDownEvent()
    {
        Selection.activeObject = this;
        //isSelected = !isSelected; //Bu da aþaðýdaki bloklarla ayný iþi görüyor.
        //Toggle node selection
        if (isSelected == true)
        {
            isSelected = false;
        }
        else
        {
            isSelected = true;
        }
    }

    private void ProcessRightClickDownEvent(Event currentEvent)
    {
        roomNodeGraph.SetNodeToDrawConnectionLineFrom(this, currentEvent.mousePosition);
    }

    private void ProcessMouseUpEvent(Event currentEvent)
    {
        if (currentEvent.button == 0)
        {
            ProcessLeftClickUpEvent();
        }
    }

    private void ProcessLeftClickUpEvent()
    {
        if (isLeftClickDragging)
        {
            isLeftClickDragging = false;
        }
    }

    private void ProcessMouseDragEvent(Event currentEvent)
    {
        if (currentEvent.button == 0)
        {
            ProcessLeftMouseDragEvent(currentEvent);
        }
    }

    private void ProcessLeftMouseDragEvent(Event currentEvent)
    {
        isLeftClickDragging = true;

        DragNode(currentEvent.delta);
        GUI.changed = true;
    }

    public void DragNode(Vector2 delta)
    {
        rect.position += delta;
        EditorUtility.SetDirty(this);
    }

    public bool AddChildRoomNodeIDToRoomNode(string childID)
    {
        if (IsChildRoomValid(childID)) //Check child node can be added validly to parent
        {
            childRoomNodeIDList.Add(childID);
            return true;
        }
        return false;
        
       
    }


    public bool IsChildRoomValid(string childID)
    {
        bool isConnectedBossNodeAlready = false;
        // check if there is there already a connected boss room in the node graph
        foreach (RoomNodeSO roomNode in roomNodeGraph.roomNodeList)
        {
            if (roomNode.roomNodeType.isBossRoom && roomNode.parentRoomNodeIDList.Count > 0) //tip boss roomsa ve 1 adet baðlantýsý yani parentý varsa;
            {
                isConnectedBossNodeAlready = true;
            }

        }
        //eðer child node boss room tipindeyse ve boss rooma baðlandýysa false dön yani 2. bir boss rooma baðlanama
        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isBossRoom && isConnectedBossNodeAlready)
            return false;
        //eðer child node baðalntýya izin vermedeiðimiz tiplerdense
        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isNone)
            return false;
        //eðer nodeumuz belirtilen child'a sahipse tekrar baðlantý yapma. 2 kere ayný nodeu baðlamamýzý engelliyor.
        if (childRoomNodeIDList.Contains(childID))
            return false;
        //eðer baðlaný kendisi ise baðlama
        if (id == childID)
            return false;
        //eðer bu childID çokten parentID listte ise baðlama. Yani bir node diðerinin parentýyken child nodeu o parenta parent yapama.
        if (parentRoomNodeIDList.Contains(childID))
            return false;
        // child node çoktan bir parenta sahipse false dön. Bu bizim design kuralýmýzda vardý.
        if (roomNodeGraph.GetRoomNode(childID).parentRoomNodeIDList.Count > 0)
            return false;
        // koridorlarý birbirine baðlamayý engelle
        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && roomNodeType.isCorridor)
            return false;
        //bizim odalarýmýz sadece koridorlar aracýlýðý ile birbirine baðlanabilir design kuralý.
        if (!roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && !roomNodeType.isCorridor)
            return false;
        //eðer izin verilenden fazla child koridor varsa baðlama.Settingste buna 3 demiþtik dizayn kuýralý bu da.

        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && childRoomNodeIDList.Count >= Settings.maxChildCorridors)
            return false;
        //eðer child room enterance ise false dön enterance her zaman en büyük parent olmalý
        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isEnterance)
            return false;
        // eðer bir roomu koridora ekliyorsak. bu koridorun çoktan bir roomu olmamasý lazým. bir koridor tek bir odaya çýkar mantýðý
        if (!roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && childRoomNodeIDList.Count > 0)
            return false;

        return true;


    }

    public bool AddParentRoomNodeIDToRoomNode(string parentID)
    {
        parentRoomNodeIDList.Add(parentID);
        return true;
    }

    public bool RemoveChildRoomNodeIDFromRoomNode(string childID)
    {
        //eðer node bir child ID barýndýrýyorsa sil onu
        if (childRoomNodeIDList.Contains(childID))
        {
            childRoomNodeIDList.Remove(childID);
            return true;
        }
        return false;
    }

    public bool RemoveParenRoomNodeIDFromRoomNode(string parentID)
    {
        //eðer node bir child ID barýndýrýyorsa sil onu
        if (parentRoomNodeIDList.Contains(parentID))
        {
            parentRoomNodeIDList.Remove(parentID);
            return true;
        }
        return false;
    }
#endif

    #endregion Editor Code
}
