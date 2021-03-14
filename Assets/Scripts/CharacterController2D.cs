using System;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.Events;
using AnyPortrait;
using DG.Tweening;
using UnityEditor;

struct BoundPosition
{
	public Vector2 topRight, topLeft;
	public Vector2 bottomLeft, bottomRight;
}
/// <summary>
/// 2D角色控制器
/// </summary>
public class CharacterController2D : MonoBehaviour
{
	public float maxSpeed = 2;
	public Transform respawnPos;
	public static CharacterController2D Insyance { get; private set; }
	[SerializeField] private float m_JumpForce = 400f;
	[SerializeField] private float m_MovementSmoothing = 10f;	// How much to smooth out the movement
	[SerializeField] private bool m_AirControl = false;			// Whether or not a player can steer while jumping;
	[SerializeField] private int groundCheakRayCount = 8;
	[SerializeField] private float skinWidth = 0.01f;
	[SerializeField] LayerMask groundLayer;
	private CapsuleCollider2D collider2D;
	private bool m_Grounded;            // Whether or not the player is grounded.
	private bool isMoving = false;
	private Rigidbody2D m_Rigidbody2D;
	private bool m_FacingRight = true;  // For determining which way the player is currently facing.
	private BoundPosition _boundPosition;
	private PlayerInput _playerInput;
	private PhysicsMaterial2D _material2D;
	private apPortrait _apPortrait;
	
	private float dressParm = 0;
	private Tweener dressTweener;
	
	public UnityEvent OnLandEvent;
	
	private void Awake()
	{
		m_Rigidbody2D = GetComponent<Rigidbody2D>();
		_material2D = m_Rigidbody2D.sharedMaterial;
		Insyance = this;
		
		if (OnLandEvent == null)
			OnLandEvent = new UnityEvent();
	}

	private void Start()
	{
		collider2D = GetComponent<CapsuleCollider2D>();
		_playerInput = PlayerInput.Instance;
		_apPortrait = GetComponentInChildren<apPortrait>();
		_apPortrait.Initialize();
		
	}

	void RecaculateBounds()
	{
		var bounds = collider2D.bounds;
		bounds.Expand(skinWidth*-2);
		_boundPosition.topRight = new Vector2(bounds.max.x,bounds.max.y);
		_boundPosition.topLeft = new Vector2(bounds.min.x,bounds.max.y);
		_boundPosition.bottomRight = new Vector2(bounds.max.x,bounds.min.y);
		_boundPosition.bottomLeft = new Vector2(bounds.min.x,bounds.min.y);
	}
	
	private void Update()
	{
		
		
		RecaculateBounds();
		m_Grounded = false;
		Vector2 offset = new Vector2(0,collider2D.size.y/2);
		
		for (int i = 0; i <= groundCheakRayCount; i++)
		{
			Debug.DrawRay(Vector3.Lerp(_boundPosition.bottomLeft +offset,_boundPosition.bottomRight+offset,(float)i/groundCheakRayCount),Vector2.down);
			RaycastHit2D hit = Physics2D.Raycast(Vector3.Lerp(_boundPosition.bottomLeft +offset,_boundPosition.bottomRight+offset,(float)i/groundCheakRayCount),Vector2.down, 0.1f +offset.y,groundLayer);
			if (hit)
			{
				m_Grounded = true;

			}
		}
		_apPortrait._animator.SetBool("Grounded",m_Grounded);

		if (m_Rigidbody2D.velocity.y < -0.1f && !m_Grounded)
		{
			DressControl(1,0.5f);
		}

		if (m_Grounded)
		{
			DressControl(0,0.5f);
		}
		
	
		_apPortrait._controller._controlParams[0].SetFloat(dressParm);
		
		Move(_playerInput.inputDir.x * 10,_playerInput.jump);

	}
	
	/// <summary>
	/// 设置摩擦力防止玩家打滑
	/// </summary>
	void PreventSlid()
	{
		if (Mathf.Abs(_playerInput.inputDir.x)>0.1f)
		{
			isMoving = true;
		}
		else
		{
			isMoving = false;
		}

		m_Rigidbody2D.sharedMaterial = collider2D.sharedMaterial = new PhysicsMaterial2D() {friction = 0};

		if (m_Grounded && !isMoving) m_Rigidbody2D.sharedMaterial = collider2D.sharedMaterial = new PhysicsMaterial2D() {friction = 10};

	}
	/// <summary>
	/// 移动函数
	/// </summary>
	/// <param name="move">移动的x值</param>
	/// <param name="jump">是否按下跳跃</param>
	public void Move(float move, bool jump)
	{
		//only control the player if grounded or airControl is turned on
		if (m_Grounded || m_AirControl)
		{
			// Move the character by finding the target velocity
			Vector3 targetVelocity = new Vector2(move, m_Rigidbody2D.velocity.y);
			m_Rigidbody2D.velocity = Vector3.Lerp(m_Rigidbody2D.velocity, targetVelocity, m_MovementSmoothing*Time.deltaTime);
			// If the input is moving the player right and the player is facing left...
			if (move > 0 && !m_FacingRight)
			{
				// ... flip the player.
				Flip();
			}
			// Otherwise if the input is moving the player left and the player is facing right...
			else if (move < 0 && m_FacingRight)
			{
				// ... flip the player.
				Flip();
			}
		}
		// If the player should jump...
		if (jump&& m_Grounded)
		{
			m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce));
			_apPortrait._animator.SetTrigger("Jump");
			DressControl(-1,0.3f);
		}
		PreventSlid();
		
		_apPortrait._animator.SetFloat("Move",Mathf.Abs(move));
		
		
	}
	/// <summary>
	/// 控制裙子的开合
	/// </summary>
	/// <param name="value">开合的目标值</param>
	/// <param name="duration">间隔</param>
	void DressControl(float value,float duration)
	{
		if(dressTweener ==null)
			dressTweener = DOTween.To(() => dressParm, (x) => dressParm = x, value, duration);
		else
		{
			dressTweener.Kill();
			dressTweener = DOTween.To(() => dressParm, (x) => dressParm = x, value, duration);
		}

	}
	/// <summary>
	/// 角色死亡
	/// </summary>
	public void PlayerDead()
	{
		PlayerInput.Instance.enabled = false;
		_apPortrait._animator.SetTrigger("Die");
		GameObject.FindObjectOfType<SwitchLevel>().ReloadScene();
		


	}
	/// <summary>
	/// 角色翻转
	/// </summary>
	private void Flip()
	{
		// Switch the way the player is labelled as facing.
		m_FacingRight = !m_FacingRight;

		// Multiply the player's x local scale by -1.
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}
}
